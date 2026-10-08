using CommerceCore.Ordering.Application;
using CommerceCore.Ordering.Infrastructure.Catalog;
using CommerceCore.Ordering.Infrastructure.Persistence;
using CommerceCore.Ordering.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CommerceCore.Ordering.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            services.AddDbContext<OrderDbContext>(options =>
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly(typeof(OrderDbContext).Assembly.FullName);
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorNumbersToAdd: null);
                }));

            services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<OrderDbContext>());
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddSingleton<IProductCatalogClient, StubProductCatalogClient>();
            return services;
        }
    }
}
