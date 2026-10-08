using CommerceCore.Ordering.Application;
using CommerceCore.Ordering.Domain;
using CommerceCore.Ordering.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommerceCore.Ordering.Infrastructure.Repositories
{
    public class OrderRepository(OrderDbContext context) : IOrderRepository
    {
        public async Task<Order?> GetByIdAsync(OrderId id, CancellationToken ct = default)
        {
            return await context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id, ct);
        }

        public async Task AddAsync(Order order, CancellationToken ct = default)
        {
            await context.Orders.AddAsync(order, ct);
        }

        public async Task<ICollection<Order>> GetAllAsync(CancellationToken ct = default)
        {
            return await context.Orders
                .Include(o => o.Items)
                .ToListAsync(ct);
        }
    }
}