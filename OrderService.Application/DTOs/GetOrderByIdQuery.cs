using MediatR;
using OrderService.Application.DTOs;

namespace OrderService.Application.Orders.Queries
{
    public record GetOrderByIdQuery(Guid OrderId) : IRequest<OrderResponse?>;
}