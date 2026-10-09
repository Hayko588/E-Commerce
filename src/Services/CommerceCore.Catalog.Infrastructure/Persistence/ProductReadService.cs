using CommerceCore.Catalog.Application;
using Microsoft.EntityFrameworkCore;

namespace CommerceCore.Catalog.Infrastructure.Persistence;

internal sealed class ProductReadService(CatalogDbContext db) : IProductReadService
{
    public Task<ProductResponse?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Products.AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new ProductResponse(p.Id, p.Sku, p.Name, p.Price.Amount, p.Price.Currency, p.IsActive))
            .FirstOrDefaultAsync(ct);

    public async Task<PagedResult<ProductResponse>> ListAsync(
        int page, int pageSize, string? search, CancellationToken ct = default)
    {
        var query = db.Products.AsNoTracking().Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p => p.Name.Contains(term) || p.Sku.Contains(term));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(p => p.Name).ThenBy(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductResponse(p.Id, p.Sku, p.Name, p.Price.Amount, p.Price.Currency, p.IsActive))
            .ToListAsync(ct);

        return new PagedResult<ProductResponse>(items, page, pageSize, total);
    }

    // Unknown and inactive products are simply absent from the result.
    public async Task<IReadOnlyDictionary<Guid, PriceResponse>> GetPricesAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
    {
        var wanted = ids.Distinct().ToList();

        var rows = await db.Products.AsNoTracking()
            .Where(p => p.IsActive && wanted.Contains(p.Id))
            .Select(p => new PriceResponse(p.Id, p.Price.Amount, p.Price.Currency))
            .ToListAsync(ct);

        return rows.ToDictionary(r => r.ProductId);
    }
}
