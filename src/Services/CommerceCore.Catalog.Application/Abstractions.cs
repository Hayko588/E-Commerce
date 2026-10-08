using CommerceCore.Catalog.Domain;

namespace CommerceCore.Catalog.Application;

// Write side
public interface IProductRepository
{
    Task<bool> SkuExistsAsync(string sku, CancellationToken ct = default);
    Task AddAsync(Product product, CancellationToken ct = default);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

// Read side: projects straight to DTOs, never loads aggregates
public interface IProductReadService
{
    Task<ProductResponse?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<ProductResponse>> ListAsync(int page, int pageSize, string? search, CancellationToken ct = default);
    Task<IReadOnlyDictionary<Guid, PriceResponse>> GetPricesAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);
}