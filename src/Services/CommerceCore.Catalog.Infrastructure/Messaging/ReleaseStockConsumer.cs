using CommerceCore.Catalog.Application.Stock;
using CommerceCore.Catalog.Infrastructure.Persistence;
using CommerceCore.Contracts;
using MassTransit;

namespace CommerceCore.Catalog.Infrastructure.Messaging;

public sealed class ReleaseStockConsumer(StockReservationService reservations, CatalogDbContext db)
    : IConsumer<ReleaseStock>
{
    public async Task Consume(ConsumeContext<ReleaseStock> context)
    {
        await reservations.ReleaseAsync(context.Message.OrderId, context.CancellationToken);
        await context.Publish(new StockReleased(context.Message.OrderId));

        // Stock levels, reservation rows and the outgoing event commit together.
        await db.SaveChangesAsync(context.CancellationToken);

        StockMetrics.Reservations.Add(1, new KeyValuePair<string, object?>("outcome", "released"));
    }
}
