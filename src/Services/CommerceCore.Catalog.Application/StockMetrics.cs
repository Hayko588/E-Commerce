using System.Diagnostics.Metrics;

namespace CommerceCore.Catalog.Application.Stock;

public static class StockMetrics
{
    private static readonly Meter Meter = new("CommerceCore.Catalog");

    public static readonly Counter<long> Reservations =
        Meter.CreateCounter<long>("commerce.stock.reservations", description: "Stock reservation outcomes");
}
