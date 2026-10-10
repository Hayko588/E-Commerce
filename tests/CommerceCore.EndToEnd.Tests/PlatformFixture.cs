using CommerceCore.Catalog.Api.Controllers;
using CommerceCore.Ordering.Api.Controllers;
using CommerceCore.Ordering.Application;
using CommerceCore.TestSupport;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Testcontainers.MsSql;
using Xunit;

namespace CommerceCore.EndToEnd.Tests;

// Two real services in one process, each with its own database, joined by a real RabbitMQ broker.
public sealed class PlatformFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _sql = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    private WebApplicationFactory<ProductsController>? _catalog;
    private WebApplicationFactory<OrdersController>? _ordering;

    public RabbitMqBroker Broker { get; } = new();
    public HttpClient Orders { get; private set; } = null!;
    public IServiceProvider CatalogServices => _catalog!.Services;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_sql.StartAsync(), Broker.StartAsync());
        Broker.ExportSettings();

        // Catalog first: its queues must exist before Ordering publishes anything.
        _catalog = Start<ProductsController>("CatalogE2eDb", _ => { });
        _ordering = Start<OrdersController>("OrdersE2eDb", services =>
        {
            services.RemoveAll<IProductCatalogClient>();
            services.AddSingleton<IProductCatalogClient, AnyProductCatalogClient>();
        });

        Orders = _ordering.CreateClient();
    }

    private WebApplicationFactory<T> Start<T>(string database, Action<IServiceCollection> configureServices) where T : class
    {
        // Each service reads its connection string while it starts,
        // so the variable is set right before each host is created.
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection",
            new SqlConnectionStringBuilder(_sql.GetConnectionString()) { InitialCatalog = database }.ConnectionString);

        var factory = new WebApplicationFactory<T>().WithWebHostBuilder(builder =>
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

        _ = factory.Server;   // starts the host: migrations, seeding and the message bus
        return factory;
    }

    public async Task DisposeAsync()
    {
        Orders.Dispose();
        if (_ordering is not null) await _ordering.DisposeAsync();
        if (_catalog is not null) await _catalog.DisposeAsync();
        await _sql.DisposeAsync();
        await Broker.DisposeAsync();
    }
}
