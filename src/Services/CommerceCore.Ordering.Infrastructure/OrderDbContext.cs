using CommerceCore.Ordering.Application;
using CommerceCore.Ordering.Domain;
using CommerceCore.Ordering.Domain.Entities;
using MassTransit;
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

            modelBuilder.AddInboxStateEntity();
            modelBuilder.AddOutboxMessageEntity();
            modelBuilder.AddOutboxStateEntity();

            base.OnModelCreating(modelBuilder);
        }
    }
}
