using MediatR;

namespace CommerceCore.Ordering.Application.DTOs
{
    public class GetAllOrdersHandler(IOrderRepository orderRepository)
        : IRequestHandler<GetAllOrders, List<OrderResponse>>
    {
        public async Task<List<OrderResponse>> Handle(GetAllOrders request, CancellationToken cancellationToken)
        {
            var orders = await orderRepository.GetAllAsync(cancellationToken);
            return orders.Select(order => new OrderResponse(
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
            )).ToList();

        }
    }
}
