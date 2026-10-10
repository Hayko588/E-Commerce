using CommerceCore.Catalog.Infrastructure.Persistence;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CommerceCore.Catalog.Infrastructure.Messaging;

public static class MessagingRegistration
{
    public static IServiceCollection AddCatalogMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        var rabbit = configuration.GetSection("RabbitMq");
        string Required(string key) => rabbit[key]
            ?? throw new InvalidOperationException($"Configuration value 'RabbitMq:{key}' is missing.");

        services.AddMassTransit(x =>
        {
            x.SetKebabCaseEndpointNameFormatter();

            x.AddConsumer<ReserveStockConsumer>();

            x.AddEntityFrameworkOutbox<CatalogDbContext>(o =>
            {
                o.UseSqlServer();
                o.QueryDelay = TimeSpan.FromSeconds(1);
                o.DuplicateDetectionWindow = TimeSpan.FromMinutes(30);
            });

            // Consumers run inside the transactional outbox + inbox (idempotent processing).
            x.AddConfigureEndpointsCallback((context, name, cfg) =>
                cfg.UseEntityFrameworkOutbox<CatalogDbContext>(context));

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(Required("Host"), rabbit["VirtualHost"] ?? "/", h =>
                {
                    h.Username(Required("Username"));
                    h.Password(Required("Password"));
                });

                cfg.UseMessageRetry(r => r.Exponential(
                    retryLimit: 5,
                    minInterval: TimeSpan.FromSeconds(1),
                    maxInterval: TimeSpan.FromSeconds(30),
                    intervalDelta: TimeSpan.FromSeconds(2)));

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
