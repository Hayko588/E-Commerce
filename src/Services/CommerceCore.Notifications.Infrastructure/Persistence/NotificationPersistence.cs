using CommerceCore.Notifications.Application;
using CommerceCore.Notifications.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommerceCore.Notifications.Infrastructure.Persistence;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).ValueGeneratedNever();

        builder.Property(n => n.Recipient).HasMaxLength(200).IsRequired();
        builder.Property(n => n.Subject).HasMaxLength(200).IsRequired();
        builder.Property(n => n.Body).HasMaxLength(2000).IsRequired();
        builder.Property(n => n.SentAtUtc).HasPrecision(3);

        // One notification per order and kind: the database enforces the idempotency the code relies on.
        builder.HasIndex(n => new { n.OrderId, n.Kind }).IsUnique();
    }
}

internal sealed class NotificationRepository(NotificationsDbContext db) : INotificationRepository
{
    public Task<bool> ExistsAsync(Guid orderId, NotificationKind kind, CancellationToken ct = default) =>
        db.Notifications.AnyAsync(n => n.OrderId == orderId && n.Kind == kind, ct);

    public void Add(Notification notification) => db.Notifications.Add(notification);
}

internal sealed class NotificationReadService(NotificationsDbContext db) : INotificationReadService
{
    public async Task<IReadOnlyList<NotificationResponse>> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default)
    {
        var rows = await db.Notifications.AsNoTracking()
            .Where(n => n.OrderId == orderId)
            .OrderBy(n => n.SentAtUtc)
            .ToListAsync(ct);

        return [.. rows.Select(n => new NotificationResponse(n.Kind.ToString(), n.Recipient, n.Subject, n.Body, n.SentAtUtc))];
    }
}
