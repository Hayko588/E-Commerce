using CommerceCore.Contracts;
using CommerceCore.Ordering.Application;
using CommerceCore.Ordering.Domain;
using CommerceCore.Ordering.Domain.Enums;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace CommerceCore.Ordering.Infrastructure.Messaging;

public sealed class OrderStockReservedConsumer(IOrderRepository orders, IUnitOfWork unitOfWork)
    : IConsumer<OrderStockReserved>
{
    public async Task Consume(ConsumeContext<OrderStockReserved> context)
    {
        var order = await orders.GetByIdAsync(new OrderId(context.Message.OrderId), context.CancellationToken)
            ?? throw new InvalidOperationException($"Order {context.Message.OrderId} not found.");

        if (order.Status != OrderStatus.Pending) return;   // duplicate, or the order is already further along

        order.MarkStockReserved();
        await unitOfWork.SaveChangesAsync(context.CancellationToken);
    }
}

public sealed class OrderConfirmedConsumer(IOrderRepository orders, IUnitOfWork unitOfWork)
    : IConsumer<OrderConfirmed>
{
    public async Task Consume(ConsumeContext<OrderConfirmed> context)
    {
        var order = await orders.GetByIdAsync(new OrderId(context.Message.OrderId), context.CancellationToken)
            ?? throw new InvalidOperationException($"Order {context.Message.OrderId} not found.");

        if (order.Status == OrderStatus.Paid) return;   // duplicate delivery

        order.MarkPaid();
        await unitOfWork.SaveChangesAsync(context.CancellationToken);
    }
}

public sealed class OrderRejectedConsumer(
    IOrderRepository orders, IUnitOfWork unitOfWork, ILogger<OrderRejectedConsumer> logger)
    : IConsumer<OrderRejected>
{
    public async Task Consume(ConsumeContext<OrderRejected> context)
    {
        var order = await orders.GetByIdAsync(new OrderId(context.Message.OrderId), context.CancellationToken)
            ?? throw new InvalidOperationException($"Order {context.Message.OrderId} not found.");

        if (order.Status == OrderStatus.Cancelled) return;   // duplicate delivery

        order.Cancel();
        await unitOfWork.SaveChangesAsync(context.CancellationToken);

        logger.LogWarning("Order {OrderId} cancelled: {Reason}", context.Message.OrderId, context.Message.Reason);
    }
}
