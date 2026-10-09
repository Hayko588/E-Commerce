using CommerceCore.Ordering.Application;
using CommerceCore.Ordering.Domain;

namespace CommerceCore.Ordering.IntegrationTests;

internal sealed class FakeProductCatalogClient : IProductCatalogClient
{
    private static readonly IReadOnlyDictionary<Guid, Money> Prices = new Dictionary<Guid, Money>
    {
        [Guid.Parse("11111111-1111-1111-1111-111111111111")] = new(49.99m, "USD"),
        [Guid.Parse("22222222-2222-2222-2222-222222222222")] = new(19.90m, "USD"),
        [Guid.Parse("33333333-3333-3333-3333-333333333333")] = new(199.00m, "USD"),
    };

    public Task<IReadOnlyDictionary<Guid, Money>> GetPricesAsync(
        IReadOnlyCollection<Guid> productIds, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyDictionary<Guid, Money>>(
            productIds.Distinct().Where(Prices.ContainsKey).ToDictionary(id => id, id => Prices[id]));
}

internal sealed class UnavailableProductCatalogClient : IProductCatalogClient
{
    public Task<IReadOnlyDictionary<Guid, Money>> GetPricesAsync(
        IReadOnlyCollection<Guid> productIds, CancellationToken ct = default) =>
        throw new CatalogUnavailableException("Simulated outage.");
}
