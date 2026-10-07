using MediatR;
using OrderService.Application.Orders.Queries;
using OrderService.Domain;

namespace OrderService.Application.DTOs
{
    public class GetOrderByIdQueryHandler(IOrderRepository orderRepository)
        : IRequestHandler<GetOrderByIdQuery, OrderResponse?>
    {
        public async Task<OrderResponse?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
        {
            var order = await orderRepository.GetByIdAsync(new OrderId(request.OrderId), cancellationToken);

            if (order is null)
                return null;

            return new OrderResponse(
                order.Id.Value,
                order.CustomerId,
                order.Status.ToString(),
                order.TotalAmount.Amount,
                order.TotalAmount.Currency,
                [.. order.Items.Select(i => new OrderItemResponse(
                i.ProductId.Value,
                i.Quantity,
                i.UnitPrice.Amount,
                i.TotalPrice.Amount
            ))],
                order.CreatedAtUtc
            );
        }
    }
}