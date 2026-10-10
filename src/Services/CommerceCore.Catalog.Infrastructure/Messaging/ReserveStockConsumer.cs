using CommerceCore.Catalog.Application.Stock;
using CommerceCore.Catalog.Infrastructure.Persistence;
using CommerceCore.Contracts;
using MassTransit;

namespace CommerceCore.Catalog.Infrastructure.Messaging;

public sealed class ReserveStockConsumer(StockReservationService reservations, CatalogDbContext db)
    : IConsumer<ReserveStock>
{
    public async Task Consume(ConsumeContext<ReserveStock> context)
    {
        var message = context.Message;

        var outcome = await reservations.ReserveAsync(
            message.OrderId,
            [.. message.Items.Select(i => new ReserveItem(i.ProductId, i.Quantity))],
            context.CancellationToken);

        if (outcome.Reserved)
            await context.Publish(new StockReserved(message.OrderId));
        else
            await context.Publish(new StockReservationRejected(message.OrderId, outcome.RejectionReason!));

        // Stock change, reservation rows and the outgoing event commit in one transaction (outbox).
        // A concurrent reservation of the same item throws DbUpdateConcurrencyException here;
        // the retry policy redelivers the message and it is evaluated against the fresh stock level.
        await db.SaveChangesAsync(context.CancellationToken);
    }
}
