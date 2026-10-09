using CommerceCore.Catalog.Application.Products;
using Shouldly;
using Xunit;

namespace CommerceCore.Catalog.UnitTests;

public class CreateProductCommandValidatorTests
{
    private readonly CreateProductCommandValidator _validator = new();

    private static CreateProductCommand Valid() => new("KB-001", "Keyboard", 49.99m, "USD");

    [Fact]
    public void A_well_formed_command_is_valid() =>
        _validator.Validate(Valid()).IsValid.ShouldBeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Price_must_be_positive(int price) =>
        _validator.Validate(Valid() with { Price = price })
            .Errors.ShouldContain(e => e.PropertyName == "Price");

    [Theory]
    [InlineData("")]
    [InlineData("US")]
    [InlineData("USDX")]
    public void Currency_must_have_three_letters(string currency) =>
        _validator.Validate(Valid() with { Currency = currency })
            .Errors.ShouldContain(e => e.PropertyName == "Currency");

    [Fact]
    public void Sku_is_required_and_limited_to_32_characters()
    {
        _validator.Validate(Valid() with { Sku = "" }).IsValid.ShouldBeFalse();
        _validator.Validate(Valid() with { Sku = new string('X', 33) }).IsValid.ShouldBeFalse();
    }
}
