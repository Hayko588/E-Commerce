using CommerceCore.Ordering.Application.DTOs;
using CommerceCore.Ordering.Domain;
using CommerceCore.Ordering.Domain.Entities;
using CommerceCore.Ordering.Domain.Exceptions;
using MediatR;

namespace CommerceCore.Ordering.Application.Commands
{
    public record CreateOrderCommand(
        Guid CustomerId,
        List<CreateOrderItemRequest> Items
    ) : IRequest<Guid>;

    public class CreateOrderCommandHandler(
        IOrderRepository orderRepository,
        IProductCatalogClient catalog,
        IUnitOfWork unitOfWork) : IRequestHandler<CreateOrderCommand, Guid>
    {
        public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
        {
            var productIds = request.Items.Select(i => i.ProductId).ToList();
            var prices = await catalog.GetPricesAsync(productIds, cancellationToken);

            var unknown = productIds.Where(id => !prices.ContainsKey(id)).ToList();
            if (unknown.Count > 0)
                throw new DomainException($"Unknown or unavailable products: {string.Join(", ", unknown)}");

            var items = request.Items
                .Select(i => new OrderItem(new ProductId(i.ProductId), prices[i.ProductId], i.Quantity))
                .ToArray();

            var order = new Order(request.CustomerId, items);

            await orderRepository.AddAsync(order, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return order.Id.Value;
        }
    }
}
