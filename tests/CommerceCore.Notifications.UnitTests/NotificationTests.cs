using CommerceCore.Notifications.Application;
using CommerceCore.Notifications.Domain;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CommerceCore.Notifications.UnitTests;

public class NotificationTests
{
    private static Notification New(Guid? orderId = null, Guid? customerId = null, string subject = "Hello") =>
        Notification.Create(orderId ?? Guid.NewGuid(), customerId ?? Guid.NewGuid(),
            NotificationKind.OrderConfirmed, "someone@example.test", subject, "Body");

    [Fact]
    public void A_new_notification_is_not_sent_yet() =>
        New().SentAtUtc.ShouldBeNull();

    [Fact]
    public void Marking_sent_records_the_first_time_only()
    {
        var notification = New();
        var first = new DateTime(2026, 10, 12, 9, 0, 0, DateTimeKind.Utc);

        notification.MarkSent(first);
        notification.MarkSent(first.AddHours(1));

        notification.SentAtUtc.ShouldBe(first);
    }

    [Fact]
    public void An_order_id_is_required() =>
        Should.Throw<DomainException>(() => New(orderId: Guid.Empty));

    [Fact]
    public void A_customer_id_is_required() =>
        Should.Throw<DomainException>(() => New(customerId: Guid.Empty));

    [Fact]
    public void A_subject_is_required() =>
        Should.Throw<DomainException>(() => New(subject: " "));
}

public class NotificationServiceTests
{
    private readonly INotificationRepository _notifications = Substitute.For<INotificationRepository>();
    private readonly INotificationSender _sender = Substitute.For<INotificationSender>();
    private readonly NotificationService _service;

    public NotificationServiceTests() => _service = new(_notifications, _sender, TimeProvider.System);

    [Fact]
    public async Task A_confirmation_is_sent_to_the_customer_and_recorded_as_sent()
    {
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        await _service.NotifyOrderConfirmedAsync(orderId, customerId);

        await _sender.Received(1).SendAsync(
            Arg.Is<Notification>(n => n.Kind == NotificationKind.OrderConfirmed
                                      && n.OrderId == orderId
                                      && n.Recipient == $"customer-{customerId:N}@example.test"),
            Arg.Any<CancellationToken>());
        _notifications.Received(1).Add(Arg.Is<Notification>(n => n.SentAtUtc != null));
    }

    [Fact]
    public async Task A_rejection_tells_the_customer_why()
    {
        await _service.NotifyOrderRejectedAsync(Guid.NewGuid(), Guid.NewGuid(), "Payment failed: Card declined.");

        await _sender.Received(1).SendAsync(
            Arg.Is<Notification>(n => n.Kind == NotificationKind.OrderRejected
                                      && n.Body.Contains("Payment failed: Card declined.")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_notification_that_already_exists_is_not_sent_again()
    {
        var orderId = Guid.NewGuid();
        _notifications.ExistsAsync(orderId, NotificationKind.OrderConfirmed, Arg.Any<CancellationToken>()).Returns(true);

        await _service.NotifyOrderConfirmedAsync(orderId, Guid.NewGuid());

        await _sender.DidNotReceiveWithAnyArgs().SendAsync(default!, default);
        _notifications.DidNotReceiveWithAnyArgs().Add(default!);
    }

    [Fact]
    public async Task A_failing_sender_leaves_nothing_recorded()
    {
        _sender.SendAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("smtp down")));

        await Should.ThrowAsync<InvalidOperationException>(
            () => _service.NotifyOrderConfirmedAsync(Guid.NewGuid(), Guid.NewGuid()));

        _notifications.DidNotReceiveWithAnyArgs().Add(default!);   // the message will be retried
    }
}
