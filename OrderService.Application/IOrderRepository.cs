using OrderService.Domain;

namespace OrderService.Application
{
    public interface IOrderRepository
    {
        Task<Order?> GetByIdAsync(OrderId id, CancellationToken ct = default);
        Task<ICollection<Order>> GetAllAsync(CancellationToken ct = default);
        Task AddAsync(Order order, CancellationToken ct = default);
    }
}
