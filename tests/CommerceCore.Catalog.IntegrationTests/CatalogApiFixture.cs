using CommerceCore.Catalog.Infrastructure.Persistence;
using CommerceCore.TestSupport;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;
using Testcontainers.MsSql;
using Xunit;

namespace CommerceCore.Catalog.IntegrationTests;

public sealed class CatalogApiFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _sql = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();
    private readonly RabbitMqBroker _broker = new();

    private WebApplicationFactory<Program>? _factory;

    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_sql.StartAsync(), _broker.StartAsync());

        var connectionString = new SqlConnectionStringBuilder(_sql.GetConnectionString())
        {
            InitialCatalog = "CatalogTestDb"
        }.ConnectionString;

        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", connectionString);
        _broker.ExportSettings();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureLogging(logging => { logging.ClearProviders(); logging.AddConsole(); });
            });

        Client = _factory.CreateClient();   // startup applies migrations and seeds the sample products

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        (await db.Database.GetAppliedMigrationsAsync())
            .ShouldNotBeEmpty("No EF migrations were applied. Are the migration files (including *.Designer.cs) present?");
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        if (_factory is not null) await _factory.DisposeAsync();
        await _sql.DisposeAsync();
        await _broker.DisposeAsync();
    }
}
