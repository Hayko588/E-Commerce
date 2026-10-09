using System.Net;
using System.Text;
using CommerceCore.Ordering.Application;
using CommerceCore.Ordering.Infrastructure.Catalog;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace CommerceCore.Ordering.UnitTests;

public class HttpProductCatalogClientTests
{
    private static readonly Guid Keyboard = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        private int _calls;
        public int Calls => _calls;
        public Uri? LastUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Interlocked.Increment(ref _calls);
            LastUri = request.RequestUri;
            return Task.FromResult(respond(request));
        }
    }

    private static HttpProductCatalogClient ClientFor(StubHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("http://catalog") },
            NullLogger<HttpProductCatalogClient>.Instance);

    private static HttpResponseMessage Json(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    [Fact]
    public async Task Known_products_are_mapped_to_money()
    {
        var handler = new StubHandler(_ =>
            Json($$"""[{"productId":"{{Keyboard}}","amount":49.99,"currency":"USD"}]"""));

        var prices = await ClientFor(handler).GetPricesAsync([Keyboard, Guid.NewGuid()]);

        var price = prices.ShouldHaveSingleItem();
        price.Key.ShouldBe(Keyboard);
        price.Value.Amount.ShouldBe(49.99m);
        price.Value.Currency.ShouldBe("USD");
        handler.LastUri!.AbsolutePath.ShouldBe("/internal/v1/prices");
        handler.LastUri.Query.ShouldContain($"ids={Keyboard}");
    }

    [Fact]
    public async Task No_ids_means_no_http_call()
    {
        var handler = new StubHandler(_ => Json("[]"));

        var prices = await ClientFor(handler).GetPricesAsync([]);

        prices.ShouldBeEmpty();
        handler.Calls.ShouldBe(0);
    }

    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task Server_errors_become_CatalogUnavailable(HttpStatusCode status)
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(status));

        await Should.ThrowAsync<CatalogUnavailableException>(
            () => ClientFor(handler).GetPricesAsync([Keyboard]));
    }

    [Fact]
    public async Task Network_failures_become_CatalogUnavailable()
    {
        var handler = new StubHandler(_ => throw new HttpRequestException("connection refused"));

        await Should.ThrowAsync<CatalogUnavailableException>(
            () => ClientFor(handler).GetPricesAsync([Keyboard]));
    }

    [Fact]
    public async Task Circuit_opens_after_repeated_failures_and_stops_calling_catalog()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpClient<IProductCatalogClient, HttpProductCatalogClient>(
                c => c.BaseAddress = new Uri("http://catalog"))
            .ConfigurePrimaryHttpMessageHandler(() => handler)
            .AddStandardResilienceHandler(o =>
            {
                HttpProductCatalogClient.ConfigureResilience(o);   // the production settings...
                o.Retry.Delay = TimeSpan.Zero;                     // ...minus the waiting, to keep the test fast
                o.Retry.UseJitter = false;
            });
        await using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IProductCatalogClient>();

        for (var i = 0; i < 10; i++)
            await Should.ThrowAsync<CatalogUnavailableException>(() => client.GetPricesAsync([Guid.NewGuid()]));

        var callsWhenOpen = handler.Calls;
        await Should.ThrowAsync<CatalogUnavailableException>(() => client.GetPricesAsync([Guid.NewGuid()]));

        handler.Calls.ShouldBe(callsWhenOpen);   // open circuit: this request never reached the handler
        handler.Calls.ShouldBeLessThan(30);      // without the breaker: 10 calls x 3 attempts = 30
    }
}
