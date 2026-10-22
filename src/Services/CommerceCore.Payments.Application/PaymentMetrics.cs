using System.Diagnostics.Metrics;

namespace CommerceCore.Payments.Application;

public static class PaymentMetrics
{
    private static readonly Meter Meter = new("CommerceCore.Payments");

    public static readonly Counter<long> Processed =
        Meter.CreateCounter<long>("commerce.payments.processed", description: "Payments attempted, by outcome");
}
