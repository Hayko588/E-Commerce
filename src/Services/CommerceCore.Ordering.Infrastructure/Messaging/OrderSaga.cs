using CommerceCore.Contracts;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommerceCore.Ordering.Infrastructure.Messaging;

public sealed class OrderState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }          // equals the OrderId
    public string CurrentState { get; set; } = null!;
    public Guid CustomerId { get; set; }
    public decimal Total { get; set; }
    public string Currency { get; set; } = "";
    public string? PaymentToken { get; set; }        // cleared as soon as the charge has been requested
    public string? FailureReason { get; set; }
    public byte[] RowVersion { get; set; } = [];     // optimistic concurrency for saga rows
}

public sealed class OrderStateConfiguration : IEntityTypeConfiguration<OrderState>
{
    public void Configure(EntityTypeBuilder<OrderState> builder)
    {
        builder.ToTable("OrderSagas");
        builder.HasKey(x => x.CorrelationId);
        builder.Property(x => x.CorrelationId).ValueGeneratedNever();
        builder.Property(x => x.CurrentState).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Total).HasPrecision(18, 2);
        builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        builder.Property(x => x.PaymentToken).HasMaxLength(64);
        builder.Property(x => x.FailureReason).HasMaxLength(256);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => x.CurrentState);
    }
}

// Orchestrates one order: reserve stock, charge the payment, and undo the reservation if the charge fails.
public sealed class OrderStateMachine : MassTransitStateMachine<OrderState>
{
    public State ReservingStock { get; private set; } = null!;
    public State Charging { get; private set; } = null!;
    public State Compensating { get; private set; } = null!;
    public State Confirmed { get; private set; } = null!;
    public State Rejected { get; private set; } = null!;

    public Event<OrderSubmitted> Submitted { get; private set; } = null!;
    public Event<StockReserved> StockWasReserved { get; private set; } = null!;
    public Event<StockReservationRejected> StockWasRejected { get; private set; } = null!;
    public Event<PaymentSucceeded> PaymentWasSuccessful { get; private set; } = null!;
    public Event<PaymentFailed> PaymentWasDeclined { get; private set; } = null!;
    public Event<StockReleased> StockWasReleased { get; private set; } = null!;

    public OrderStateMachine()
    {
        InstanceState(x => x.CurrentState);

        Event(() => Submitted, e => e.CorrelateById(m => m.Message.OrderId));
        Event(() => StockWasReserved, e => e.CorrelateById(m => m.Message.OrderId));
        Event(() => StockWasRejected, e => e.CorrelateById(m => m.Message.OrderId));
        Event(() => PaymentWasSuccessful, e => e.CorrelateById(m => m.Message.OrderId));
        Event(() => PaymentWasDeclined, e => e.CorrelateById(m => m.Message.OrderId));
        Event(() => StockWasReleased, e => e.CorrelateById(m => m.Message.OrderId));

        Initially(
            When(Submitted)
                .Then(ctx =>
                {
                    ctx.Saga.CustomerId = ctx.Message.CustomerId;
                    ctx.Saga.Total = ctx.Message.Total;
                    ctx.Saga.Currency = ctx.Message.Currency;
                    ctx.Saga.PaymentToken = ctx.Message.PaymentToken;
                })
                .Publish(ctx => new ReserveStock(ctx.Saga.CorrelationId, ctx.Message.Items))
                .TransitionTo(ReservingStock));

        During(ReservingStock,
            When(StockWasReserved)
                .Publish(ctx => new OrderStockReserved(ctx.Saga.CorrelationId))
                .Publish(ctx => new ChargePayment(
                    ctx.Saga.CorrelationId, ctx.Saga.Total, ctx.Saga.Currency, ctx.Saga.PaymentToken!))
                .Then(ctx => ctx.Saga.PaymentToken = null)
                .TransitionTo(Charging),
            When(StockWasRejected)
                .Then(ctx => ctx.Saga.FailureReason = Truncate(ctx.Message.Reason, 256))
                .Publish(ctx => new OrderRejected(ctx.Saga.CorrelationId, ctx.Message.Reason))
                .TransitionTo(Rejected),
            Ignore(Submitted));

        During(Charging,
            When(PaymentWasSuccessful)
                .Publish(ctx => new OrderConfirmed(ctx.Saga.CorrelationId))
                .TransitionTo(Confirmed),
            When(PaymentWasDeclined)
                .Then(ctx => ctx.Saga.FailureReason = Truncate($"Payment failed: {ctx.Message.Reason}", 256))
                .Publish(ctx => new ReleaseStock(ctx.Saga.CorrelationId))      // the compensating action
                .TransitionTo(Compensating),
            Ignore(Submitted),
            Ignore(StockWasReserved));

        During(Compensating,
            When(StockWasReleased)
                .Publish(ctx => new OrderRejected(
                    ctx.Saga.CorrelationId, ctx.Saga.FailureReason ?? "Payment failed."))
                .TransitionTo(Rejected),
            Ignore(Submitted),
            Ignore(StockWasReserved),
            Ignore(PaymentWasDeclined));

        // At-least-once delivery: duplicates of events that were already handled are ignored.
        During(Confirmed, Rejected,
            Ignore(Submitted),
            Ignore(StockWasReserved),
            Ignore(StockWasRejected),
            Ignore(PaymentWasSuccessful),
            Ignore(PaymentWasDeclined),
            Ignore(StockWasReleased));
    }

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];
}
