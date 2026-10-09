using System.Net.Http.Json;
using CommerceCore.Ordering.Application;
using CommerceCore.Ordering.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace CommerceCore.Ordering.Infrastructure.Catalog;

public sealed class HttpProductCatalogClient(HttpClient http, ILogger<HttpProductCatalogClient> logger)
    : IProductCatalogClient
{
    // Ordering's own view of Catalog's response. Deliberately not shared with the Catalog project.
    private sealed record PriceDto(Guid ProductId, decimal Amount, string Currency);

    public async Task<IReadOnlyDictionary<Guid, Money>> GetPricesAsync(
        IReadOnlyCollection<Guid> productIds, CancellationToken ct = default)
    {
        var ids = productIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, Money>();

        var url = "internal/v1/prices?" + string.Join("&", ids.Select(id => $"ids={id}"));

        try
        {
            using var response = await http.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException(
                    $"Catalog returned {(int)response.StatusCode} {response.ReasonPhrase}.", null, response.StatusCode);

            var prices = await response.Content.ReadFromJsonAsync<List<PriceDto>>(ct) ?? [];

            return prices.ToDictionary(p => p.ProductId, p => new Money(p.Amount, p.Currency));
        }
        catch (Exception ex) when (IsTransient(ex, ct))
        {
            logger.LogWarning("Catalog price lookup failed: {Reason}", ex.Message);
            throw new CatalogUnavailableException("The product catalog is unavailable.", ex);
        }
    }

    // Transport-level failures only. If the caller itself cancelled, let that propagate.
    private static bool IsTransient(Exception ex, CancellationToken ct) =>
        ex is HttpRequestException or BrokenCircuitException or TimeoutRejectedException
        || (ex is OperationCanceledException && !ct.IsCancellationRequested);

    public static void ConfigureResilience(HttpStandardResilienceOptions options)
    {
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(3);
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(10);

        options.Retry.MaxRetryAttempts = 2;
        options.Retry.BackoffType = DelayBackoffType.Exponential;
        options.Retry.Delay = TimeSpan.FromMilliseconds(200);
        options.Retry.UseJitter = true;

        // Open the circuit when half of at least 5 attempts in 30s failed, then stay open for 15s.
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
        options.CircuitBreaker.MinimumThroughput = 5;
        options.CircuitBreaker.FailureRatio = 0.5;
        options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);
    }
}

public static class CatalogClientRegistration
{
    public static IServiceCollection AddCatalogClient(this IServiceCollection services, IConfiguration configuration)
    {
        var baseUrl = configuration["Services:Catalog:BaseUrl"]
            ?? throw new InvalidOperationException("Configuration value 'Services:Catalog:BaseUrl' is missing.");

        services.AddHttpClient<IProductCatalogClient, HttpProductCatalogClient>(
                client => client.BaseAddress = new Uri(baseUrl))
            .AddStandardResilienceHandler(HttpProductCatalogClient.ConfigureResilience);

        return services;
    }
}
