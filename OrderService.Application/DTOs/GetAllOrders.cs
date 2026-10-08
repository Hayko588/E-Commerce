using MediatR;

namespace OrderService.Application.DTOs
{
    public record GetAllOrders : IRequest<List<OrderResponse?>>;
}
