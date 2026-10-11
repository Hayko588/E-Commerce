namespace CommerceCore.Notifications.Domain;

public class DomainException(string message) : Exception(message);

public enum NotificationKind
{
    OrderConfirmed = 1,
    OrderRejected = 2
}

public sealed class Notification
{
    private Notification() { }   // for EF Core

    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid CustomerId { get; private set; }
    public NotificationKind Kind { get; private set; }
    public string Recipient { get; private set; } = null!;
    public string Subject { get; private set; } = null!;
    public string Body { get; private set; } = null!;
    public DateTime? SentAtUtc { get; private set; }

    public static Notification Create(
        Guid orderId, Guid customerId, NotificationKind kind, string recipient, string subject, string body)
    {
        if (orderId == Guid.Empty) throw new DomainException("Order id is required.");
        if (customerId == Guid.Empty) throw new DomainException("Customer id is required.");
        if (string.IsNullOrWhiteSpace(recipient)) throw new DomainException("Recipient is required.");
        if (string.IsNullOrWhiteSpace(subject)) throw new DomainException("Subject is required.");
        if (string.IsNullOrWhiteSpace(body)) throw new DomainException("Body is required.");

        return new Notification
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            CustomerId = customerId,
            Kind = kind,
            Recipient = recipient,
            Subject = subject,
            Body = body
        };
    }

    public void MarkSent(DateTime nowUtc) => SentAtUtc ??= nowUtc;
}
