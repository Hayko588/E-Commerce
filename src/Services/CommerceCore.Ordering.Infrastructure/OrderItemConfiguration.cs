using CommerceCore.Ordering.Domain;
using CommerceCore.Ordering.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommerceCore.Ordering.Infrastructure.Persistence.Configurations
{
    public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
    {
        public void Configure(EntityTypeBuilder<OrderItem> builder)
        {
            builder.ToTable("OrderItems");

            builder.HasKey(i => i.Id);

            builder.Property(i => i.ProductId)
                .HasConversion(
                    id => id.Value,
                    value => new ProductId(value))
                .IsRequired();

            builder.ComplexProperty(i => i.UnitPrice, money =>
            {
                money.Property(m => m.Amount)
                    .HasColumnName("UnitPriceAmount")
                    .HasPrecision(18, 2)
                    .IsRequired();

                money.Property(m => m.Currency)
                    .HasColumnName("UnitPriceCurrency")
                    .HasMaxLength(3)
                    .IsRequired();
            });

            builder.Property(i => i.Quantity)
                .IsRequired();
        }
    }
}
