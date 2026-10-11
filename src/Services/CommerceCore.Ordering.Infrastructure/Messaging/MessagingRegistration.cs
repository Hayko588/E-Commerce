using CommerceCore.Ordering.Application;
using CommerceCore.Ordering.Infrastructure.Persistence;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CommerceCore.Ordering.Infrastructure.Messaging;

internal sealed class MassTransitPublisher(IPublishEndpoint publishEndpoint) : IMessagePublisher
{
    public Task PublishAsync<T>(T message, CancellationToken ct = default) where T : class =>
        publishEndpoint.Publish(message, ct);
}

public static class MessagingRegistration
{
    public static IServiceCollection AddOrderingMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        var rabbit = configuration.GetSection("RabbitMq");
        string Required(string key) => rabbit[key]
            ?? throw new InvalidOperationException($"Configuration value 'RabbitMq:{key}' is missing.");

        services.AddScoped<IMessagePublisher, MassTransitPublisher>();

        services.AddMassTransit(x =>
        {
            x.SetKebabCaseEndpointNameFormatter();

            x.AddConsumer<OrderConfirmedConsumer>();
            x.AddConsumer<OrderRejectedConsumer>();
            x.AddConsumer<OrderStockReservedConsumer>();

            x.AddSagaStateMachine<OrderStateMachine, OrderState>()
                .EntityFrameworkRepository(r =>
                {
                    r.ConcurrencyMode = ConcurrencyMode.Optimistic;
                    r.ExistingDbContext<OrderDbContext>();
                    r.UseSqlServer();
                });

            x.AddEntityFrameworkOutbox<OrderDbContext>(o =>
            {
                o.UseSqlServer();
                o.UseBusOutbox();   // IPublishEndpoint used from the API writes to the outbox table
                o.QueryDelay = TimeSpan.FromSeconds(1);
                o.DuplicateDetectionWindow = TimeSpan.FromMinutes(30);
            });

            x.AddConfigureEndpointsCallback((context, name, cfg) =>
                cfg.UseEntityFrameworkOutbox<OrderDbContext>(context));

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(Required("Host"), ushort.Parse(rabbit["Port"] ?? "5672"), rabbit["VirtualHost"] ?? "/", h =>
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
