using CommerceCore.Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace CommerceCore.Catalog.Infrastructure.Persistence;

// Development-only sample data.
public static class CatalogSeeder
{
    public static async Task SeedAsync(CatalogDbContext db, TimeProvider clock, CancellationToken ct = default)
    {
        if (await db.Products.AnyAsync(ct)) return;

        db.Products.AddRange(
            Product.Create(Guid.Parse("11111111-1111-1111-1111-111111111111"), "KB-001", "Mechanical keyboard", new Money(49.99m), clock),
            Product.Create(Guid.Parse("22222222-2222-2222-2222-222222222222"), "MS-001", "Wireless mouse", new Money(19.90m), clock),
            Product.Create(Guid.Parse("33333333-3333-3333-3333-333333333333"), "MN-001", "27-inch monitor", new Money(199.00m), clock));

        await db.SaveChangesAsync(ct);
    }
}
