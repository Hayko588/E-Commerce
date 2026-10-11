using CommerceCore.Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommerceCore.Catalog.Infrastructure.Persistence;

public sealed class StockItemConfiguration : IEntityTypeConfiguration<StockItem>
{
    public void Configure(EntityTypeBuilder<StockItem> builder)
    {
        builder.ToTable("StockItems", t => t.HasCheckConstraint(
            "CK_StockItems_Quantities", "[OnHand] >= 0 AND [Reserved] >= 0 AND [Reserved] <= [OnHand]"));

        builder.HasKey(s => s.ProductId);
        builder.Property(s => s.ProductId).ValueGeneratedNever();
        builder.Property(s => s.RowVersion).IsRowVersion();   // optimistic concurrency
        builder.Ignore(s => s.Available);

        builder.HasOne<Product>().WithOne().HasForeignKey<StockItem>(s => s.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class StockReservationConfiguration : IEntityTypeConfiguration<StockReservation>
{
    public void Configure(EntityTypeBuilder<StockReservation> builder)
    {
        builder.ToTable("StockReservations");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.CreatedAtUtc).HasPrecision(3);
        builder.Property(r => r.ReleasedAtUtc).HasPrecision(3);

        // One reservation per product per order: the database enforces idempotency.
        builder.HasIndex(r => new { r.OrderId, r.ProductId }).IsUnique();

        builder.HasOne<Product>().WithMany().HasForeignKey(r => r.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
