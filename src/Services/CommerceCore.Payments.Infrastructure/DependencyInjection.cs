using CommerceCore.Payments.Application;
using CommerceCore.Payments.Infrastructure.Gateway;
using CommerceCore.Payments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CommerceCore.Payments.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPaymentsInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<PaymentsDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsAssembly(typeof(PaymentsDbContext).Assembly.FullName);
                sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null);
                sql.CommandTimeout(120);
            }));

        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IPaymentReadService, PaymentReadService>();
        services.AddSingleton<IPaymentGateway, SimulatedPaymentGateway>();

        return services;
    }
}
