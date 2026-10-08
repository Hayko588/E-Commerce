using CommerceCore.Ordering.Application;
using CommerceCore.Ordering.Domain;

namespace CommerceCore.Ordering.Infrastructure.Catalog;

// Temporary stand-in for the Catalog service. Replaced by an HTTP client later.
public sealed class StubProductCatalogClient : IProductCatalogClient
{
    private static readonly IReadOnlyDictionary<Guid, Money> Prices = new Dictionary<Guid, Money>
    {
        [Guid.Parse("11111111-1111-1111-1111-111111111111")] = new(49.99m, "USD"),   // Keyboard
        [Guid.Parse("22222222-2222-2222-2222-222222222222")] = new(19.90m, "USD"),   // Mouse
        [Guid.Parse("33333333-3333-3333-3333-333333333333")] = new(199.00m, "USD"),  // Monitor
    };

    public Task<IReadOnlyDictionary<Guid, Money>> GetPricesAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken ct = default)
    {
        var found = productIds
            .Distinct()
            .Where(Prices.ContainsKey)
            .ToDictionary(id => id, id => Prices[id]);

        return Task.FromResult<IReadOnlyDictionary<Guid, Money>>(found);
    }
}
