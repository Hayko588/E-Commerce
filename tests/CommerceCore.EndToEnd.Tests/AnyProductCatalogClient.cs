using CommerceCore.Ordering.Application;
using CommerceCore.Ordering.Domain;

namespace CommerceCore.EndToEnd.Tests;

// Ordering's price lookup is covered elsewhere. Here every product costs 10.00 USD,
// so the tests can create products and stock directly in the Catalog database.
internal sealed class AnyProductCatalogClient : IProductCatalogClient
{
    public Task<IReadOnlyDictionary<Guid, Money>> GetPricesAsync(
        IReadOnlyCollection<Guid> productIds, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyDictionary<Guid, Money>>(
            productIds.Distinct().ToDictionary(id => id, _ => new Money(10m, "USD")));
}
