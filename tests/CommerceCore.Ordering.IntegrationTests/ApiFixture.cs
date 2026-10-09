using CommerceCore.Ordering.Application;
using CommerceCore.Ordering.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shouldly;
using Testcontainers.MsSql;
using Xunit;

namespace CommerceCore.Ordering.IntegrationTests;

public sealed class ApiFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _sql = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    private WebApplicationFactory<Program>? _factory;

    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _sql.StartAsync();

        var connectionString = new SqlConnectionStringBuilder(_sql.GetConnectionString())
        {
            InitialCatalog = "OrdersTestDb"
        }.ConnectionString;

        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", connectionString);

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureTestServices(services => Replace<FakeProductCatalogClient>(services));
            });

        Client = _factory.CreateClient();   // startup applies migrations

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        (await db.Database.GetAppliedMigrationsAsync())
            .ShouldNotBeEmpty("No EF migrations were applied. Are the migration files (including *.Designer.cs) present?");
    }

    // A client whose host uses a different IProductCatalogClient implementation.
    public HttpClient CreateClientWithCatalog<T>() where T : class, IProductCatalogClient =>
        _factory!.WithWebHostBuilder(b => b.ConfigureTestServices(services => Replace<T>(services))).CreateClient();

    private static void Replace<T>(IServiceCollection services) where T : class, IProductCatalogClient
    {
        services.RemoveAll<IProductCatalogClient>();
        services.AddSingleton<IProductCatalogClient, T>();
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        if (_factory is not null) await _factory.DisposeAsync();
        await _sql.DisposeAsync();
    }
}
