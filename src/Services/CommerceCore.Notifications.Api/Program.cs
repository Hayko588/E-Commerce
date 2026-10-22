using CommerceCore.BuildingBlocks.Telemetry;
using CommerceCore.Notifications.Api;
using CommerceCore.Notifications.Application;
using CommerceCore.Notifications.Infrastructure;
using CommerceCore.Notifications.Infrastructure.Messaging;
using CommerceCore.Notifications.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddSingleton(TimeProvider.System);
builder.Services
    .AddNotificationsApplication()
    .AddNotificationsInfrastructure(connectionString)
    .AddNotificationsMessaging(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks().AddDbContextCheck<NotificationsDbContext>("db", tags: ["db"]);
builder.Services.AddCommerceTelemetry(builder.Configuration, "notifications-api");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => options.WithTitle("Commerce Core - Notifications API"));
}

app.UseExceptionHandler();

app.MapControllers();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("db") });

if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
    await db.MigrateWithRetryAsync(app.Logger);
}

app.Run();

public partial class Program { }
