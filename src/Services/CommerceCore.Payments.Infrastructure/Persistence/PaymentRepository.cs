using CommerceCore.Payments.Application;
using CommerceCore.Payments.Domain;
using Microsoft.EntityFrameworkCore;

namespace CommerceCore.Payments.Infrastructure.Persistence;

internal sealed class PaymentRepository(PaymentsDbContext db) : IPaymentRepository
{
    public Task<Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default) =>
        db.Payments.FirstOrDefaultAsync(p => p.OrderId == orderId, ct);

    public void Add(Payment payment) => db.Payments.Add(payment);
}

internal sealed class PaymentReadService(PaymentsDbContext db) : IPaymentReadService
{
    public async Task<PaymentResponse?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default)
    {
        var p = await db.Payments.AsNoTracking().FirstOrDefaultAsync(x => x.OrderId == orderId, ct);

        return p is null
            ? null
            : new PaymentResponse(p.Id, p.OrderId, p.Amount, p.Currency, p.Status.ToString(),
                p.ProviderReference, p.FailureReason, p.CreatedAtUtc);
    }
}
