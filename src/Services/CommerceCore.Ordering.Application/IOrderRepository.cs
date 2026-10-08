using CommerceCore.Ordering.Domain;

namespace CommerceCore.Ordering.Application
{
    public interface IOrderRepository
    {
        Task<Order?> GetByIdAsync(OrderId id, CancellationToken ct = default);
        Task<ICollection<Order>> GetAllAsync(CancellationToken ct = default);
        Task AddAsync(Order order, CancellationToken ct = default);
    }
}
