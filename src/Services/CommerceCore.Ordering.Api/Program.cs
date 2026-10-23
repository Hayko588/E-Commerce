using CommerceCore.BuildingBlocks.Telemetry;
using CommerceCore.Ordering.Api.Middleware;
using CommerceCore.Ordering.Application;
using CommerceCore.Ordering.Infrastructure;
using CommerceCore.Ordering.Infrastructure.Messaging;
using CommerceCore.Ordering.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplicationServices()
    .AddInfrastructureServices(builder.Configuration)
    .AddOrderingMessaging(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHealthChecks().AddDbContextCheck<OrderDbContext>("db", tags: ["db"]);

builder.Services.AddCommerceTelemetry(builder.Configuration, "ordering-api");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Commerce Core - Order Service API")
               .WithTheme(ScalarTheme.Mars);
    });
}

app.UseExceptionHandler();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("db") });

app.Run();

public partial class Program { }
