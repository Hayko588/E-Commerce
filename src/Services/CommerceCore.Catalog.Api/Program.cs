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

if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    await db.Database.MigrateAsync();
    await CatalogSeeder.SeedAsync(db, scope.ServiceProvider.GetRequiredService<TimeProvider>());
}

app.Run();

public partial class Program { }
