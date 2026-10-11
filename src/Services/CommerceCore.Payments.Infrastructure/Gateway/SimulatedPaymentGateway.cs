using CommerceCore.Payments.Application;

namespace CommerceCore.Payments.Infrastructure.Gateway;

// Stands in for a real provider (Stripe, Adyen...). The token decides the outcome.
public sealed class SimulatedPaymentGateway : IPaymentGateway
{
    public Task<GatewayResult> ChargeAsync(
        string idempotencyKey, decimal amount, string currency, string paymentToken, CancellationToken ct = default)
    {
        var result = paymentToken switch
        {
            "tok_ok" => GatewayResult.Approve($"sim_{idempotencyKey[..12]}"),
            "tok_decline" => GatewayResult.Decline("Card declined."),
            "tok_insufficient_funds" => GatewayResult.Decline("Insufficient funds."),
            _ => GatewayResult.Decline("Unknown payment token.")
        };

        return Task.FromResult(result);
    }
}
