using CommerceCore.Ordering.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommerceCore.Ordering.Infrastructure.Persistence.Configurations
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.ToTable("Orders");

            builder.HasKey(o => o.Id);

            builder.Property(o => o.Id)
                .HasConversion(
                    id => id.Value,
                    value => new OrderId(value))
                .ValueGeneratedNever();

            builder.Property(o => o.CustomerId)
                .IsRequired();

            builder.Property(o => o.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(o => o.CreatedAtUtc)
                .IsRequired();

            builder.Property(o => o.UpdatedAtUtc)
                .IsRequired(false);

            builder.HasMany(o => o.Items)
                .WithOne()
                .HasForeignKey("OrderId")
                .OnDelete(DeleteBehavior.Cascade);

            builder.Navigation(o => o.Items)
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            builder.Ignore(o => o.DomainEvents);
        }
    }
}