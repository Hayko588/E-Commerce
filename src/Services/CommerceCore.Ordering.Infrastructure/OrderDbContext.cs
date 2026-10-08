using CommerceCore.Ordering.Application;
using CommerceCore.Ordering.Domain;
using CommerceCore.Ordering.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CommerceCore.Ordering.Infrastructure.Persistence
{
    public class OrderDbContext(DbContextOptions<OrderDbContext> options)
        : DbContext(options), IUnitOfWork
    {
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrderDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }

        public async override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}
