using CommerceCore.Notifications.Domain;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace CommerceCore.Notifications.Application;

public interface INotificationSender
{
    Task SendAsync(Notification notification, CancellationToken ct = default);
}

public interface INotificationRepository
{
    Task<bool> ExistsAsync(Guid orderId, NotificationKind kind, CancellationToken ct = default);
    void Add(Notification notification);
}

public sealed record NotificationResponse(string Kind, string Recipient, string Subject, string Body, DateTime? SentAtUtc);

public interface INotificationReadService
{
    Task<IReadOnlyList<NotificationResponse>> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default);
}

public sealed record GetNotificationsByOrderQuery(Guid OrderId) : IRequest<IReadOnlyList<NotificationResponse>>;

public sealed class GetNotificationsByOrderQueryHandler(INotificationReadService reads)
    : IRequestHandler<GetNotificationsByOrderQuery, IReadOnlyList<NotificationResponse>>
{
    public Task<IReadOnlyList<NotificationResponse>> Handle(GetNotificationsByOrderQuery request, CancellationToken ct) =>
        reads.GetByOrderIdAsync(request.OrderId, ct);
}

// Nothing is saved here: the consumer saves, so the record and the consumed message commit together.
public sealed class NotificationService(
    INotificationRepository notifications, INotificationSender sender, TimeProvider clock)
{
    public Task NotifyOrderConfirmedAsync(Guid orderId, Guid customerId, CancellationToken ct = default) =>
        NotifyAsync(NotificationKind.OrderConfirmed, orderId, customerId,
            "Your order is confirmed",
            $"Thank you! Order {orderId} has been paid and is being prepared.", ct);

    public Task NotifyOrderRejectedAsync(Guid orderId, Guid customerId, string reason, CancellationToken ct = default) =>
        NotifyAsync(NotificationKind.OrderRejected, orderId, customerId,
            "We could not complete your order",
            $"Order {orderId} was cancelled. Reason: {reason}", ct);

    private async Task NotifyAsync(
        NotificationKind kind, Guid orderId, Guid customerId, string subject, string body, CancellationToken ct)
    {
        // At most one notification per order and kind, however many times the event is delivered.
        if (await notifications.ExistsAsync(orderId, kind, ct)) return;

        // Contact details would come from a customer or identity service. A derived address stands in for them.
        var notification = Notification.Create(
            orderId, customerId, kind, $"customer-{customerId:N}@example.test", subject, body);

        await sender.SendAsync(notification, ct);
        notification.MarkSent(clock.GetUtcNow().UtcDateTime);
        notifications.Add(notification);
    }
}

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationsApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddScoped<NotificationService>();
        return services;
    }
}
