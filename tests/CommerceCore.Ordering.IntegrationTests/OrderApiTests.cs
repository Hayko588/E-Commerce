using CommerceCore.Ordering.Application.DTOs;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace CommerceCore.Ordering.IntegrationTests;

public class OrderApiTests(ApiFixture api) : IClassFixture<ApiFixture>
{
    private const string Keyboard = "11111111-1111-1111-1111-111111111111";
    private const string UnknownProduct = "99999999-9999-9999-9999-999999999999";

    // Clients may still send a price. It must be ignored.
    private static object Body(string productId, int quantity) => new
    {
        customerId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
        items = new[] { new { productId, quantity, unitPrice = 0.01m } }
    };

    [Fact]
    public async Task Created_order_uses_the_server_side_price()
    {
        var create = await api.Client.PostAsJsonAsync("/api/v1/orders", Body(Keyboard, 2));

        create.StatusCode.ShouldBe(HttpStatusCode.Created);
        create.Headers.Location.ShouldNotBeNull();

        var order = await api.Client.GetFromJsonAsync<OrderResponse>(create.Headers.Location);

        order.ShouldNotBeNull();
        order.Status.ShouldBe("Pending");
        order.TotalAmount.ShouldBe(99.98m);   // 2 x 49.99, not 2 x 0.01
        order.Currency.ShouldBe("USD");
    }

    [Fact]
    public async Task Invalid_quantity_returns_400_problem_details()
    {
        var response = await api.Client.PostAsJsonAsync("/api/v1/orders", Body(Keyboard, 0));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        (await response.Content.ReadAsStringAsync()).ShouldContain("Items[0].Quantity");
    }

    [Fact]
    public async Task Unknown_product_returns_422()
    {
        var response = await api.Client.PostAsJsonAsync("/api/v1/orders", Body(UnknownProduct, 1));

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Unknown_order_returns_404()
    {
        var response = await api.Client.GetAsync($"/api/v1/orders/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Catalog_outage_returns_503_with_retry_after()
    {
        var client = api.CreateClientWithCatalog<UnavailableProductCatalogClient>();

        var response = await client.PostAsJsonAsync("/api/v1/orders", Body(Keyboard, 1));

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        response.Headers.RetryAfter.ShouldNotBeNull();
    }
}
