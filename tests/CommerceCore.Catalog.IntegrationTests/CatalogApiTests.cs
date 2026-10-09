using System.Net;
using System.Net.Http.Json;
using CommerceCore.Catalog.Application;
using Shouldly;
using Xunit;

namespace CommerceCore.Catalog.IntegrationTests;

// One SQL Server container shared by every test class in this collection.
[CollectionDefinition("catalog-api")]
public sealed class CatalogApiCollection : ICollectionFixture<CatalogApiFixture>;

[Collection("catalog-api")]
public class CatalogApiTests(CatalogApiFixture api)
{
    private const string KeyboardId = "11111111-1111-1111-1111-111111111111";
    private const string UnknownId = "99999999-9999-9999-9999-999999999999";

    private sealed record Created(Guid Id);

    private static string UniqueSku() => "T-" + Guid.NewGuid().ToString("N")[..12];

    private Task<HttpResponseMessage> CreateProduct(string sku, decimal price = 59m) =>
        api.Client.PostAsJsonAsync("/api/v1/products",
            new { sku, name = "Headset", price, currency = "usd" });

    [Fact]
    public async Task Seeded_products_are_listed()
    {
        var page = await api.Client.GetFromJsonAsync<PagedResult<ProductResponse>>("/api/v1/products?pageSize=100");

        page.ShouldNotBeNull();
        page.TotalCount.ShouldBeGreaterThanOrEqualTo(3);
        page.Items.ShouldContain(p => p.Sku == "KB-001");
    }

    [Fact]
    public async Task Seeded_product_can_be_fetched_by_id()
    {
        var product = await api.Client.GetFromJsonAsync<ProductResponse>($"/api/v1/products/{KeyboardId}");

        product.ShouldNotBeNull();
        product.Sku.ShouldBe("KB-001");
        product.Price.ShouldBe(49.99m);
        product.Currency.ShouldBe("USD");
    }

    [Fact]
    public async Task Unknown_product_returns_404()
    {
        var response = await api.Client.GetAsync($"/api/v1/products/{UnknownId}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Created_product_is_normalised_and_retrievable()
    {
        var sku = UniqueSku();

        var create = await CreateProduct(sku);

        create.StatusCode.ShouldBe(HttpStatusCode.Created);
        create.Headers.Location.ShouldNotBeNull();

        var product = await api.Client.GetFromJsonAsync<ProductResponse>(create.Headers.Location);
        product.ShouldNotBeNull();
        product.Sku.ShouldBe(sku.ToUpperInvariant());
        product.Currency.ShouldBe("USD");
        product.Price.ShouldBe(59m);
    }

    [Fact]
    public async Task Duplicate_sku_returns_409_problem_details()
    {
        var sku = UniqueSku();
        (await CreateProduct(sku)).StatusCode.ShouldBe(HttpStatusCode.Created);

        var duplicate = await CreateProduct(sku);

        duplicate.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        duplicate.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task Non_positive_price_returns_400()
    {
        var response = await CreateProduct(UniqueSku(), price: 0m);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).ShouldContain("Price");
    }

    [Fact]
    public async Task Page_size_above_100_returns_400()
    {
        var response = await api.Client.GetAsync("/api/v1/products?pageSize=101");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Search_finds_a_product_by_sku()
    {
        var sku = UniqueSku();
        (await CreateProduct(sku)).StatusCode.ShouldBe(HttpStatusCode.Created);

        var page = await api.Client.GetFromJsonAsync<PagedResult<ProductResponse>>(
            $"/api/v1/products?search={sku.ToUpperInvariant()}");

        page.ShouldNotBeNull();
        page.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Internal_price_lookup_returns_only_known_products()
    {
        var prices = await api.Client.GetFromJsonAsync<PriceResponse[]>(
            $"/internal/v1/prices?ids={KeyboardId}&ids={UnknownId}");

        prices.ShouldNotBeNull();
        var keyboard = prices.ShouldHaveSingleItem();
        keyboard.ProductId.ShouldBe(Guid.Parse(KeyboardId));
        keyboard.Amount.ShouldBe(49.99m);
        keyboard.Currency.ShouldBe("USD");
    }

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task Health_endpoints_report_healthy(string path)
    {
        var response = await api.Client.GetAsync(path);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
