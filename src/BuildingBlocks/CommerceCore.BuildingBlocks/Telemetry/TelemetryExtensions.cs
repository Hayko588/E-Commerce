using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace CommerceCore.BuildingBlocks.Telemetry;

public static class TelemetryExtensions
{
    // One line per service. Business meters are named CommerceCore.<Area> and are picked up by the wildcard.
    public static IServiceCollection AddCommerceTelemetry(
        this IServiceCollection services, IConfiguration configuration, string serviceName)
    {
        var telemetry = services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing => tracing
                // Health probes run every few seconds; tracing them would bury the real requests.
                .AddAspNetCoreInstrumentation(o => o.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/health"))
                .AddHttpClientInstrumentation()
                .AddSqlClientInstrumentation()
                .AddSource("MassTransit"))                       // consume/publish spans, carried across RabbitMQ
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter("MassTransit", "CommerceCore.*"))
            .WithLogging();                                      // ILogger output, with the current trace id attached

        // Export only when an endpoint is configured. Tests and a plain "dotnet run" stay quiet.
        // The standard OTEL_EXPORTER_OTLP_* environment variables do the rest.
        if (!string.IsNullOrWhiteSpace(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
            telemetry.UseOtlpExporter();

        return services;
    }
}
