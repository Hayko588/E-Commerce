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
        builder.Property(x => x.FailureReason).HasMaxLength(256);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => x.CurrentState);
    }
}

// Orchestrates one order: reserve stock, then confirm or reject.
// Step 13 adds payment between "reserve" and "confirm", plus the compensating release of stock.
public sealed class OrderStateMachine : MassTransitStateMachine<OrderState>
{
    public State ReservingStock { get; private set; } = null!;
    public State Confirmed { get; private set; } = null!;
    public State Rejected { get; private set; } = null!;

    public Event<OrderSubmitted> Submitted { get; private set; } = null!;
    public Event<StockReserved> StockWasReserved { get; private set; } = null!;
    public Event<StockReservationRejected> StockWasRejected { get; private set; } = null!;

    public OrderStateMachine()
    {
        InstanceState(x => x.CurrentState);

        Event(() => Submitted, e => e.CorrelateById(m => m.Message.OrderId));
        Event(() => StockWasReserved, e => e.CorrelateById(m => m.Message.OrderId));
        Event(() => StockWasRejected, e => e.CorrelateById(m => m.Message.OrderId));

        Initially(
            When(Submitted)
                .Then(ctx => ctx.Saga.CustomerId = ctx.Message.CustomerId)
                .Publish(ctx => new ReserveStock(ctx.Saga.CorrelationId, ctx.Message.Items))
                .TransitionTo(ReservingStock));

        During(ReservingStock,
            When(StockWasReserved)
                .Publish(ctx => new OrderConfirmed(ctx.Saga.CorrelationId))
                .TransitionTo(Confirmed),
            When(StockWasRejected)
                .Then(ctx => ctx.Saga.FailureReason = Truncate(ctx.Message.Reason, 256))
                .Publish(ctx => new OrderRejected(ctx.Saga.CorrelationId, ctx.Message.Reason))
                .TransitionTo(Rejected),
            Ignore(Submitted));

        // At-least-once delivery: duplicates of events that were already handled are ignored.
        During(Confirmed, Rejected,
            Ignore(Submitted),
            Ignore(StockWasReserved),
            Ignore(StockWasRejected));
    }

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];
}
