using CommerceCore.Catalog.Api.Controllers;
using CommerceCore.Catalog.Infrastructure.Persistence;
using CommerceCore.Notifications.Api.Controllers;
using CommerceCore.Notifications.Infrastructure.Persistence;
using CommerceCore.Ordering.Api.Controllers;
using CommerceCore.Ordering.Application;
using CommerceCore.Ordering.Infrastructure.Persistence;
using CommerceCore.Payments.Api.Controllers;
using CommerceCore.Payments.Infrastructure.Persistence;
using CommerceCore.TestSupport;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Testcontainers.MsSql;
using Xunit;

namespace CommerceCore.EndToEnd.Tests;

// Four real services in one process, each with its own database, joined by a real RabbitMQ broker.
public sealed class PlatformFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _sql = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    private WebApplicationFactory<ProductsController>? _catalog;
    private WebApplicationFactory<PaymentsController>? _payments;
    private WebApplicationFactory<NotificationsController>? _notifications;
    private WebApplicationFactory<OrdersController>? _ordering;

    public RabbitMqBroker Broker { get; } = new();
    public HttpClient Orders { get; private set; } = null!;
    public HttpClient Payments { get; private set; } = null!;
    public HttpClient NotificationsApi { get; private set; } = null!;
    public IServiceProvider CatalogServices => _catalog!.Services;
    public IServiceProvider PaymentsServices => _payments!.Services;
    public IServiceProvider NotificationsServices => _notifications!.Services;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_sql.StartAsync(), Broker.StartAsync());
        Broker.ExportSettings();

        // Consumers first: their queues must exist before Ordering publishes anything.
        _catalog = await StartAsync<ProductsController, CatalogDbContext>("CatalogE2eDb", _ => { });
        _payments = await StartAsync<PaymentsController, PaymentsDbContext>("PaymentsE2eDb", _ => { });
        _notifications = await StartAsync<NotificationsController, NotificationsDbContext>("NotificationsE2eDb", _ => { });
        _ordering = await StartAsync<OrdersController, OrderDbContext>("OrdersE2eDb", services =>
        {
            services.RemoveAll<IProductCatalogClient>();
            services.AddSingleton<IProductCatalogClient, AnyProductCatalogClient>();
        });

        Orders = _ordering.CreateClient();
        Payments = _payments.CreateClient();
        NotificationsApi = _notifications.CreateClient();
    }

    private async Task<WebApplicationFactory<TEntry>> StartAsync<TEntry, TContext>(
        string database, Action<IServiceCollection> configureServices)
        where TEntry : class
        where TContext : DbContext
    {
        var connectionString = new SqlConnectionStringBuilder(_sql.GetConnectionString())
        {
            InitialCatalog = database
        }.ConnectionString;

        // Schema first, exactly as in a deployment. The application itself never migrates.
        await DatabaseMigrator.MigrateAsync<TContext>(connectionString);

        // Each service reads its connection string while it starts,
        // so the variable is set right before each host is created.
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", connectionString);

        var factory = new WebApplicationFactory<TEntry>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureLogging(logging => { logging.ClearProviders(); logging.AddConsole(); });
            builder.ConfigureTestServices(services =>
            {
                // Wait for the bus to connect before the host reports started. Otherwise a message could be
                // published before the consumer's queue is bound, and RabbitMQ drops unroutable messages.
                services.Configure<MassTransitHostOptions>(o => o.WaitUntilStarted = true);
                configureServices(services);
            });
        });

        _ = factory.Server;   // starts the host: seeding and the message bus
        return factory;
    }

    public async Task DisposeAsync()
    {
        Orders.Dispose();
        Payments.Dispose();
        NotificationsApi.Dispose();
        if (_ordering is not null) await _ordering.DisposeAsync();
        if (_notifications is not null) await _notifications.DisposeAsync();
        if (_payments is not null) await _payments.DisposeAsync();
        if (_catalog is not null) await _catalog.DisposeAsync();
        await _sql.DisposeAsync();
        await Broker.DisposeAsync();
    }
}
