using CommerceCore.BuildingBlocks.Telemetry;
using CommerceCore.Catalog.Api;
using CommerceCore.Catalog.Api.Middleware;
using CommerceCore.Catalog.Application;
using CommerceCore.Catalog.Infrastructure;
using CommerceCore.Catalog.Infrastructure.Messaging;
using CommerceCore.Catalog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services
    .AddCatalogApplication()
    .AddCatalogInfrastructure(connectionString)
    .AddCatalogMessaging(builder.Configuration);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHealthChecks().AddDbContextCheck<CatalogDbContext>("db", tags: ["db"]);
builder.Services.AddCommerceTelemetry(builder.Configuration, "catalog-api");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => options.WithTitle("Commerce Core - Catalog API"));
}

app.UseExceptionHandler();

app.MapControllers();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("db") });

var seed = app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Seed:Enabled");

if (app.Environment.IsDevelopment() || seed)
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

    // Development only. In the cluster, schema changes belong to the migration Job.
    if (app.Environment.IsDevelopment())
        await db.MigrateWithRetryAsync(app.Logger);

    if (seed)
        await CatalogSeeder.SeedAsync(db, scope.ServiceProvider.GetRequiredService<TimeProvider>());
}

app.Run();

public partial class Program { }
