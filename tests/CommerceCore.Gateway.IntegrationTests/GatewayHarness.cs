using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CommerceCore.Gateway.IntegrationTests;

public sealed record EchoResponse(string Service, string Path, string Query, string CorrelationId);

// A stand-in downstream service on a real local port. It echoes what it received.
public sealed class FakeService : IAsyncDisposable
{
    private readonly WebApplication _app;
    private bool _stopped;

    private FakeService(WebApplication app, string address)
    {
        _app = app;
        Address = address;
    }

    public string Address { get; }

    public static async Task<FakeService> StartAsync(string name)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");   // port 0: the OS picks a free port
        builder.Logging.ClearProviders();

        var app = builder.Build();

        app.MapGet("/health/ready", () => Results.Ok("ready"));
        app.Map("/{**path}", (HttpContext ctx) => Results.Json(new EchoResponse(
            name,
            ctx.Request.Path.Value ?? "",
            ctx.Request.QueryString.Value ?? "",
            ctx.Request.Headers["X-Correlation-Id"].ToString())));

        await app.StartAsync();

        return new FakeService(app, app.Urls.First());
    }

    public async Task StopAsync()
    {
        if (_stopped) return;
        _stopped = true;
        await _app.StopAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        await _app.DisposeAsync();
    }
}

// The gateway under test plus two fake downstream services.
public sealed class GatewayHarness : IAsyncLifetime
{
    private readonly List<WebApplicationFactory<Program>> _factories = [];

    public FakeService Catalog { get; private set; } = null!;
    public FakeService Ordering { get; private set; } = null!;
    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Catalog = await FakeService.StartAsync("catalog");
        Ordering = await FakeService.StartAsync("ordering");
        Client = CreateClient();
    }

    public HttpClient CreateClient(int? permitLimit = null)
    {
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ReverseProxy:Clusters:catalog:Destinations:primary:Address"] = Catalog.Address,
                    ["ReverseProxy:Clusters:ordering:Destinations:primary:Address"] = Ordering.Address
                }));

            if (permitLimit is not null)
                builder.ConfigureTestServices(services =>
                    services.Configure<RateLimitSettings>(o => o.PermitLimit = permitLimit.Value));
        });

        _factories.Add(factory);
        return factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        foreach (var factory in _factories) await factory.DisposeAsync();
        await Catalog.DisposeAsync();
        await Ordering.DisposeAsync();
    }
}
