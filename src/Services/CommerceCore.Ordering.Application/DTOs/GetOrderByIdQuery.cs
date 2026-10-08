using MediatR;

namespace CommerceCore.Ordering.Application.DTOs
{
    public record GetOrderByIdQuery(Guid OrderId) : IRequest<OrderResponse?>;
}