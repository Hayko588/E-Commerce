using CommerceCore.Catalog.Application.Stock;
using CommerceCore.Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace CommerceCore.Catalog.Infrastructure.Persistence;

internal sealed class StockRepository(CatalogDbContext db) : IStockRepository
{
    public Task<bool> HasReservationAsync(Guid orderId, CancellationToken ct = default) =>
        db.StockReservations.AnyAsync(r => r.OrderId == orderId, ct);

    // Tracked on purpose: the aggregate is modified and saved by the caller.
    public Task<Dictionary<Guid, StockItem>> GetStockAsync(
        IReadOnlyCollection<Guid> productIds, CancellationToken ct = default)
    {
        var ids = productIds.ToList();
        return db.StockItems.Where(s => ids.Contains(s.ProductId)).ToDictionaryAsync(s => s.ProductId, ct);
    }

    public void AddReservation(StockReservation reservation) => db.StockReservations.Add(reservation);
}
