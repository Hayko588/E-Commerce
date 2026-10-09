using CommerceCore.Catalog.Application;
using CommerceCore.Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace CommerceCore.Catalog.Infrastructure.Persistence;

internal sealed class ProductRepository(CatalogDbContext db) : IProductRepository
{
    public Task<bool> SkuExistsAsync(string sku, CancellationToken ct = default) =>
        db.Products.AnyAsync(p => p.Sku == sku, ct);

    public async Task AddAsync(Product product, CancellationToken ct = default) =>
        await db.Products.AddAsync(product, ct);
}
