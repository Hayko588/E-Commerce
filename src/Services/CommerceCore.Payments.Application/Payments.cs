using CommerceCore.Payments.Domain;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace CommerceCore.Payments.Application;

public sealed record GatewayResult(bool Approved, string? Reference, string? DeclineReason)
{
    public static GatewayResult Approve(string reference) => new(true, reference, null);
    public static GatewayResult Decline(string reason) => new(false, null, reason);
}

public interface IPaymentGateway
{
    // The idempotency key makes a retried charge safe: the provider returns the original result.
    Task<GatewayResult> ChargeAsync(
        string idempotencyKey, decimal amount, string currency, string paymentToken, CancellationToken ct = default);
}

public interface IPaymentRepository
{
    Task<Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default);
    void Add(Payment payment);
}

public sealed record PaymentResponse(
    Guid Id, Guid OrderId, decimal Amount, string Currency, string Status,
    string? ProviderReference, string? FailureReason, DateTime CreatedAtUtc);

public interface IPaymentReadService
{
    Task<PaymentResponse?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default);
}

public sealed record GetPaymentByOrderQuery(Guid OrderId) : IRequest<PaymentResponse?>;

public sealed class GetPaymentByOrderQueryHandler(IPaymentReadService reads)
    : IRequestHandler<GetPaymentByOrderQuery, PaymentResponse?>
{
    public Task<PaymentResponse?> Handle(GetPaymentByOrderQuery request, CancellationToken ct) =>
        reads.GetByOrderIdAsync(request.OrderId, ct);
}

// Nothing is saved here: the consumer saves, so the payment row and the outgoing event commit together.
public sealed class PaymentProcessor(IPaymentRepository payments, IPaymentGateway gateway, TimeProvider clock)
{
    public async Task<Payment> ChargeAsync(
        Guid orderId, decimal amount, string currency, string paymentToken, CancellationToken ct = default)
    {
        // Redelivered message: the same order always gets the same outcome and is never charged twice.
        var existing = await payments.GetByOrderIdAsync(orderId, ct);
        if (existing is not null) return existing;

        var result = await gateway.ChargeAsync(orderId.ToString("N"), amount, currency, paymentToken, ct);
        var now = clock.GetUtcNow().UtcDateTime;

        var payment = result.Approved
            ? Payment.Succeeded(orderId, amount, currency, result.Reference!, now)
            : Payment.Failed(orderId, amount, currency, result.DeclineReason ?? "Payment declined.", now);

        payments.Add(payment);
        PaymentMetrics.Processed.Add(1, new KeyValuePair<string, object?>("status", payment.Status.ToString().ToLowerInvariant()));
        return payment;
    }
}

public static class DependencyInjection
{
    public static IServiceCollection AddPaymentsApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddScoped<PaymentProcessor>();
        return services;
    }
}
