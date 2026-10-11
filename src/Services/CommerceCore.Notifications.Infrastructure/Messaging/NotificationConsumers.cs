using CommerceCore.Contracts;
using CommerceCore.Notifications.Application;
using CommerceCore.Notifications.Infrastructure.Persistence;
using MassTransit;

namespace CommerceCore.Notifications.Infrastructure.Messaging;

// Queue: order-confirmed-notification. Ordering has its own order-confirmed queue, and both
// receive a copy of every OrderConfirmed event. Sharing a queue name would make them compete instead.
public sealed class OrderConfirmedNotificationConsumer(NotificationService service, NotificationsDbContext db)
    : IConsumer<OrderConfirmed>
{
    public async Task Consume(ConsumeContext<OrderConfirmed> context)
    {
        await service.NotifyOrderConfirmedAsync(
            context.Message.OrderId, context.Message.CustomerId, context.CancellationToken);

        await db.SaveChangesAsync(context.CancellationToken);
    }
}

// Queue: order-rejected-notification
public sealed class OrderRejectedNotificationConsumer(NotificationService service, NotificationsDbContext db)
    : IConsumer<OrderRejected>
{
    public async Task Consume(ConsumeContext<OrderRejected> context)
    {
        await service.NotifyOrderRejectedAsync(
            context.Message.OrderId, context.Message.CustomerId, context.Message.Reason, context.CancellationToken);

        await db.SaveChangesAsync(context.CancellationToken);
    }
}
