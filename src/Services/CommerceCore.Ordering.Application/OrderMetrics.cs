using System.Diagnostics.Metrics;

namespace CommerceCore.Ordering.Application;

public static class OrderMetrics
{
    private static readonly Meter Meter = new("CommerceCore.Ordering");

    public static readonly Counter<long> Created =
        Meter.CreateCounter<long>("commerce.orders.created", description: "Orders accepted by the API");

    public static readonly Counter<long> Finished =
        Meter.CreateCounter<long>("commerce.orders.finished", description: "Orders that reached a final state");

    public static readonly Histogram<double> ProcessingSeconds =
        Meter.CreateHistogram<double>("commerce.orders.processing.duration", unit: "s",
            description: "Time from order creation to its final outcome");
}
