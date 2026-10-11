namespace CommerceCore.Payments.Domain;

public class DomainException(string message) : Exception(message);

public enum PaymentStatus
{
    Succeeded = 1,
    Failed = 2
}

public sealed class Payment
{
    private Payment() { }   // for EF Core

    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = null!;
    public PaymentStatus Status { get; private set; }
    public string? ProviderReference { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static Payment Succeeded(Guid orderId, decimal amount, string currency, string providerReference, DateTime nowUtc) =>
        Create(orderId, amount, currency, PaymentStatus.Succeeded, providerReference, null, nowUtc);

    public static Payment Failed(Guid orderId, decimal amount, string currency, string reason, DateTime nowUtc) =>
        Create(orderId, amount, currency, PaymentStatus.Failed, null, reason, nowUtc);

    private static Payment Create(
        Guid orderId, decimal amount, string currency, PaymentStatus status,
        string? providerReference, string? failureReason, DateTime nowUtc)
    {
        if (orderId == Guid.Empty) throw new DomainException("Order id is required.");
        if (amount < 0) throw new DomainException("Amount cannot be negative.");
        if (currency is not { Length: 3 }) throw new DomainException("Currency must be a 3-letter ISO 4217 code.");

        return new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Amount = decimal.Round(amount, 2, MidpointRounding.ToEven),
            Currency = currency.ToUpperInvariant(),
            Status = status,
            ProviderReference = providerReference,
            FailureReason = failureReason,
            CreatedAtUtc = nowUtc
        };
    }
}
