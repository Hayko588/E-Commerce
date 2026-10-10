using System.Net;
using System.Net.Http.Json;
using Shouldly;
using Xunit;

namespace CommerceCore.Gateway.IntegrationTests;

public class GatewayRoutingTests(GatewayHarness gateway) : IClassFixture<GatewayHarness>
{
    [Theory]
    [InlineData("GET", "/api/v1/products", "catalog")]
    [InlineData("GET", "/api/v1/products/5f0c1c3e", "catalog")]
    [InlineData("POST", "/api/v1/orders", "ordering")]
    [InlineData("GET", "/api/v1/orders/5f0c1c3e", "ordering")]
    public async Task Requests_are_routed_to_the_owning_service(string method, string path, string expectedService)
    {
        var response = await gateway.Client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var echo = await response.Content.ReadFromJsonAsync<EchoResponse>();
        echo.ShouldNotBeNull();
        echo.Service.ShouldBe(expectedService);
        echo.Path.ShouldBe(path);
    }

    [Fact]
    public async Task Query_string_is_forwarded()
    {
        var echo = await gateway.Client.GetFromJsonAsync<EchoResponse>("/api/v1/products?search=kb&page=2");

        echo.ShouldNotBeNull();
        echo.Query.ShouldBe("?search=kb&page=2");
    }

    [Theory]
    [InlineData("/internal/v1/prices?ids=11111111-1111-1111-1111-111111111111")]
    [InlineData("/api/v1/unknown")]
    [InlineData("/api/v1/productsx")]
    [InlineData("/")]
    public async Task Paths_without_a_route_return_404(string path)
    {
        var response = await gateway.Client.GetAsync(path);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Gateway_health_is_reported()
    {
        var response = await gateway.Client.GetAsync("/health/live");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task A_correlation_id_is_generated_and_reaches_the_service()
    {
        var response = await gateway.Client.GetAsync("/api/v1/products");

        var id = response.Headers.GetValues("X-Correlation-Id").ShouldHaveSingleItem();
        id.ShouldNotBeNullOrWhiteSpace();
        (await response.Content.ReadFromJsonAsync<EchoResponse>())!.CorrelationId.ShouldBe(id);
    }

    [Fact]
    public async Task A_valid_incoming_correlation_id_is_preserved()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/products");
        request.Headers.Add("X-Correlation-Id", "demo-123");

        var response = await gateway.Client.SendAsync(request);

        response.Headers.GetValues("X-Correlation-Id").ShouldHaveSingleItem().ShouldBe("demo-123");
        (await response.Content.ReadFromJsonAsync<EchoResponse>())!.CorrelationId.ShouldBe("demo-123");
    }

    [Fact]
    public async Task A_malformed_correlation_id_is_replaced()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/products");
        request.Headers.Add("X-Correlation-Id", "not valid: has spaces");

        var response = await gateway.Client.SendAsync(request);

        response.Headers.GetValues("X-Correlation-Id").ShouldHaveSingleItem().ShouldNotBe("not valid: has spaces");
    }
}

public class GatewayResilienceTests(GatewayHarness gateway) : IClassFixture<GatewayHarness>
{
    [Fact]
    public async Task A_dead_service_does_not_take_the_other_one_down()
    {
        await gateway.Catalog.StopAsync();

        var products = await gateway.Client.GetAsync("/api/v1/products");
        var orders = await gateway.Client.GetAsync("/api/v1/orders/5f0c1c3e");

        // 502 until the first health probe marks it unhealthy, 503 afterwards.
        products.StatusCode.ShouldBeOneOf(HttpStatusCode.BadGateway, HttpStatusCode.ServiceUnavailable);
        orders.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}

public class GatewayRateLimitTests(GatewayHarness gateway) : IClassFixture<GatewayHarness>
{
    [Fact]
    public async Task Requests_over_the_limit_get_429_with_retry_after()
    {
        var client = gateway.CreateClient(permitLimit: 3);

        for (var i = 0; i < 3; i++)
            (await client.GetAsync("/api/v1/products")).StatusCode.ShouldBe(HttpStatusCode.OK);

        var limited = await client.GetAsync("/api/v1/products");

        limited.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        limited.Headers.RetryAfter.ShouldNotBeNull();
        limited.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }
}
