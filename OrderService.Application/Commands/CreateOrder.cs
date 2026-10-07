using MediatR;
using OrderService.Application.DTOs;
using OrderService.Domain;
using OrderService.Domain.Entities;

namespace OrderService.Application.Orders.Commands
{
    public record CreateOrderCommand(
        Guid CustomerId,
        List<CreateOrderItemRequest> Items
    ) : IRequest<Guid>;

    public class CreateOrderCommandHandler(
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork) : IRequestHandler<CreateOrderCommand, Guid>
    {
        public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
        {
            List<OrderItem> items = [
                .. request.Items.Select(i => new OrderItem(
                new ProductId(i.ProductId),
                new Money(i.UnitPrice, i.Currency),
                i.Quantity))
            ];

            var order = new Order(request.CustomerId, [.. items]);

            await orderRepository.AddAsync(order, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return order.Id.Value;
        }
    }
}