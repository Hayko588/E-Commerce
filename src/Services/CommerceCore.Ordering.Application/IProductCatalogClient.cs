using CommerceCore.Ordering.Domain;

namespace CommerceCore.Ordering.Application
{
    public interface IProductCatalogClient
    {
        /// <summary>Returns the current price of each known product. Unknown products are absent from the result.</summary>
        Task<IReadOnlyDictionary<Guid, Money>> GetPricesAsync(
            IReadOnlyCollection<Guid> productIds,
            CancellationToken ct = default);
    }
}
