using CommerceCore.Catalog.Domain;

namespace CommerceCore.Catalog.Application.Stock;

public sealed record ReserveItem(Guid ProductId, int Quantity);

public sealed record ReservationOutcome(bool Reserved, string? RejectionReason)
{
    public static ReservationOutcome Success() => new(true, null);
    public static ReservationOutcome Rejected(string reason) => new(false, reason);
}

public interface IStockRepository
{
    Task<bool> HasReservationAsync(Guid orderId, CancellationToken ct = default);
    Task<Dictionary<Guid, StockItem>> GetStockAsync(IReadOnlyCollection<Guid> productIds, CancellationToken ct = default);
    void AddReservation(StockReservation reservation);
}

public sealed class StockReservationService(IStockRepository stock, TimeProvider clock)
{
    // All-or-nothing: either every line is reserved or none is.
    public async Task<ReservationOutcome> ReserveAsync(
        Guid orderId, IReadOnlyList<ReserveItem> items, CancellationToken ct = default)
    {
        // Redelivered message: the work is already done.
        if (await stock.HasReservationAsync(orderId, ct))
            return ReservationOutcome.Success();

        if (items.Count == 0 || items.Any(i => i.Quantity <= 0))
            return ReservationOutcome.Rejected("Invalid reservation request.");

        var wanted = items.GroupBy(i => i.ProductId).ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));
        var levels = await stock.GetStockAsync(wanted.Keys.ToList(), ct);

        var problems = new List<string>();
        foreach (var (productId, quantity) in wanted)
        {
            if (!levels.TryGetValue(productId, out var level))
                problems.Add($"{productId}: no stock record");
            else if (!level.CanReserve(quantity))
                problems.Add($"{productId}: requested {quantity}, available {level.Available}");
        }

        if (problems.Count > 0)
            return ReservationOutcome.Rejected("Insufficient stock: " + string.Join("; ", problems));

        var now = clock.GetUtcNow().UtcDateTime;
        foreach (var (productId, quantity) in wanted)
        {
            levels[productId].Reserve(quantity);
            stock.AddReservation(StockReservation.Create(orderId, productId, quantity, now));
        }

        return ReservationOutcome.Success();
    }
}
