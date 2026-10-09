using CommerceCore.Catalog.Domain;
using CommerceCore.Catalog.Domain.Exceptions;
using Shouldly;
using Xunit;

namespace CommerceCore.Catalog.UnitTests;

public class ProductTests
{
    private static Product New(string sku = " kb-001 ", string name = " Keyboard ") =>
        Product.Create(Guid.NewGuid(), sku, name, new Money(49.99m), TimeProvider.System);

    [Fact]
    public void Create_normalises_sku_and_name_and_starts_active()
    {
        var product = New();

        product.Sku.ShouldBe("KB-001");
        product.Name.ShouldBe("Keyboard");
        product.IsActive.ShouldBeTrue();
        product.Price.ShouldBe(new Money(49.99m));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_requires_a_sku(string sku) =>
        Should.Throw<DomainException>(() => New(sku: sku));

    [Fact]
    public void Create_requires_a_name() =>
        Should.Throw<DomainException>(() => New(name: " "));

    [Fact]
    public void Deactivate_turns_the_product_off()
    {
        var product = New();

        product.Deactivate();

        product.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void ChangePrice_replaces_the_price()
    {
        var product = New();

        product.ChangePrice(new Money(39.90m, "eur"));

        product.Price.ShouldBe(new Money(39.90m, "EUR"));
    }

    [Theory]
    [InlineData(-1, "USD")]
    [InlineData(1, "US")]
    [InlineData(1, "")]
    public void Money_rejects_invalid_values(int amount, string currency) =>
        Should.Throw<DomainException>(() => new Money(amount, currency));
}
