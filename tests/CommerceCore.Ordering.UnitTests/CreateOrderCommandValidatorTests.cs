using CommerceCore.Ordering.Application.Commands;
using CommerceCore.Ordering.Application.DTOs;
using Shouldly;

namespace CommerceCore.Ordering.UnitTests;

public class CreateOrderCommandValidatorTests
{
    private static readonly Guid Product = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly CreateOrderCommandValidator _validator = new();

    private static CreateOrderCommand Command(Guid customerId, params CreateOrderItemRequest[] items) =>
        new(customerId, [.. items]);

    [Fact]
    public void A_well_formed_command_is_valid()
    {
        var result = _validator.Validate(Command(Guid.NewGuid(), new CreateOrderItemRequest(Product, 2)));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Empty_customer_id_is_invalid()
    {
        var result = _validator.Validate(Command(Guid.Empty, new CreateOrderItemRequest(Product, 1)));

        result.Errors.ShouldContain(e => e.PropertyName == "CustomerId");
    }

    [Fact]
    public void An_order_without_items_is_invalid()
    {
        var result = _validator.Validate(Command(Guid.NewGuid()));

        result.Errors.ShouldContain(e => e.PropertyName == "Items");
    }

    [Fact]
    public void The_same_product_twice_is_invalid()
    {
        var result = _validator.Validate(Command(
            Guid.NewGuid(),
            new CreateOrderItemRequest(Product, 1),
            new CreateOrderItemRequest(Product, 2)));

        result.Errors.ShouldContain(e => e.PropertyName == "Items" && e.ErrorMessage.Contains("only once"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Quantity_outside_the_allowed_range_is_invalid(int quantity)
    {
        var result = _validator.Validate(Command(Guid.NewGuid(), new CreateOrderItemRequest(Product, quantity)));

        result.Errors.ShouldContain(e => e.PropertyName == "Items[0].Quantity");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void Quantity_at_the_limits_is_valid(int quantity)
    {
        var result = _validator.Validate(Command(Guid.NewGuid(), new CreateOrderItemRequest(Product, quantity)));

        result.IsValid.ShouldBeTrue();
    }
}
