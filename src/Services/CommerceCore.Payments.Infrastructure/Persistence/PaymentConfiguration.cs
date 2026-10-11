using CommerceCore.Payments.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommerceCore.Payments.Infrastructure.Persistence;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Amount).HasPrecision(18, 2);
        builder.Property(p => p.Currency).HasMaxLength(3).IsFixedLength().IsUnicode(false);
        builder.Property(p => p.ProviderReference).HasMaxLength(64);
        builder.Property(p => p.FailureReason).HasMaxLength(256);
        builder.Property(p => p.CreatedAtUtc).HasPrecision(3);

        // One payment per order: the database enforces the idempotency the code relies on.
        builder.HasIndex(p => p.OrderId).IsUnique();
    }
}
