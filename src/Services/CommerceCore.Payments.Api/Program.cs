using CommerceCore.BuildingBlocks.Telemetry;
using CommerceCore.Payments.Api;
using CommerceCore.Payments.Api.Middleware;
using CommerceCore.Payments.Application;
using CommerceCore.Payments.Infrastructure;
using CommerceCore.Payments.Infrastructure.Messaging;
using CommerceCore.Payments.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddSingleton(TimeProvider.System);
builder.Services
    .AddPaymentsApplication()
    .AddPaymentsInfrastructure(connectionString)
    .AddPaymentsMessaging(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHealthChecks().AddDbContextCheck<PaymentsDbContext>("db", tags: ["db"]);

builder.Services.AddCommerceTelemetry(builder.Configuration, "payments-api");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => options.WithTitle("Commerce Core - Payments API"));
}

app.UseExceptionHandler();

app.MapControllers();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("db") });

app.Run();

public partial class Program { }
