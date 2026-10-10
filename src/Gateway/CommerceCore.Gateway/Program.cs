using System.Text.RegularExpressions;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<RateLimitSettings>(builder.Configuration.GetSection("RateLimiting"));

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // One bucket per client IP. Behind a load balancer this will need forwarded-header handling.
    options.AddPolicy("per-ip", httpContext =>
    {
        var settings = httpContext.RequestServices.GetRequiredService<IOptions<RateLimitSettings>>().Value;

        return RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = settings.PermitLimit,
                Window = TimeSpan.FromSeconds(settings.WindowSeconds),
                QueueLimit = 0
            });
    });

    options.OnRejected = async (context, ct) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            context.HttpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();

        await context.HttpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Too many requests",
                Detail = "Rate limit exceeded. Please retry later."
            },
            options: null,
            contentType: "application/problem+json",
            ct);
    };
});

builder.Services.AddHealthChecks();

var app = builder.Build();

// Correlation id: reuse a well-formed incoming id, otherwise create one.
// The id is set on the request before proxying, so downstream services receive it too.
var validCorrelationId = new Regex("^[A-Za-z0-9_-]{1,64}$", RegexOptions.Compiled);

app.Use(async (context, next) =>
{
    const string header = "X-Correlation-Id";

    var id = context.Request.Headers[header].FirstOrDefault();
    if (id is null || !validCorrelationId.IsMatch(id))
        id = Guid.NewGuid().ToString("N");

    context.Request.Headers[header] = id;
    context.Response.OnStarting(() =>
    {
        context.Response.Headers[header] = id;
        return Task.CompletedTask;
    });

    using (app.Logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = id }))
        await next();
});

app.UseRateLimiter();

// The gateway's own health. A downstream outage must not make the gateway look unhealthy.
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

app.MapReverseProxy();

app.Run();

public sealed class RateLimitSettings
{
    public int PermitLimit { get; set; } = 100;
    public int WindowSeconds { get; set; } = 60;
}

public partial class Program { }
