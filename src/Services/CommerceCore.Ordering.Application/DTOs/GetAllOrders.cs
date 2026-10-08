using MediatR;

namespace CommerceCore.Ordering.Application.DTOs
{
    public record GetAllOrders : IRequest<List<OrderResponse?>>;
}
