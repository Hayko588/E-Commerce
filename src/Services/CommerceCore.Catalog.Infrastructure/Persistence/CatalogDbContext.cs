using CommerceCore.Catalog.Application;
using CommerceCore.Catalog.Domain;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace CommerceCore.Catalog.Infrastructure.Persistence;

// The inherited SaveChangesAsync already satisfies IUnitOfWork.
public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<StockItem> StockItems => Set<StockItem>();
    public DbSet<StockReservation> StockReservations => Set<StockReservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);

        // MassTransit tables: transactional outbox and consumer inbox (duplicate detection)
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
