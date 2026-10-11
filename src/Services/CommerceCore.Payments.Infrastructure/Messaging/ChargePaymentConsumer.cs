using CommerceCore.Contracts;
using CommerceCore.Payments.Application;
using CommerceCore.Payments.Domain;
using CommerceCore.Payments.Infrastructure.Persistence;
using MassTransit;

namespace CommerceCore.Payments.Infrastructure.Messaging;

public sealed class ChargePaymentConsumer(PaymentProcessor processor, PaymentsDbContext db)
    : IConsumer<ChargePayment>
{
    public async Task Consume(ConsumeContext<ChargePayment> context)
    {
        var message = context.Message;

        var payment = await processor.ChargeAsync(
            message.OrderId, message.Amount, message.Currency, message.PaymentToken, context.CancellationToken);

        if (payment.Status == PaymentStatus.Succeeded)
            await context.Publish(new PaymentSucceeded(message.OrderId, payment.Id));
        else
            await context.Publish(new PaymentFailed(message.OrderId, payment.FailureReason ?? "Payment failed."));

        // Payment row and outgoing event commit in one transaction (outbox).
        await db.SaveChangesAsync(context.CancellationToken);
    }
}
