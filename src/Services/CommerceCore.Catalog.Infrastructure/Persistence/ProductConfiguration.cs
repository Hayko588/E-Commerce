using CommerceCore.Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommerceCore.Catalog.Infrastructure.Persistence;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Sku).HasMaxLength(32).IsRequired();
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.CreatedAtUtc).HasPrecision(3);
        builder.Property(p => p.RowVersion).IsRowVersion();

        builder.ComplexProperty(p => p.Price, money =>
        {
            money.Property(m => m.Amount).HasColumnName("PriceAmount").HasPrecision(18, 2);
            money.Property(m => m.Currency).HasColumnName("PriceCurrency")
                .HasMaxLength(3).IsFixedLength().IsUnicode(false);
        });

        builder.HasIndex(p => p.Sku).IsUnique();
        builder.HasIndex(p => new { p.IsActive, p.Name });   // serves the paged, name-ordered list
    }
}
