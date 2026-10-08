using CommerceCore.Catalog.Application;
using CommerceCore.Catalog.Application.Products;
using CommerceCore.Catalog.Domain;
using CommerceCore.Catalog.Domain.Exceptions;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CommerceCore.Catalog.UnitTests;

public class CreateProductCommandHandlerTests
{
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CreateProductCommandHandler _handler;

    public CreateProductCommandHandlerTests() =>
        _handler = new(_products, _unitOfWork, TimeProvider.System);

    [Fact]
    public async Task A_new_product_is_saved_with_a_normalised_sku()
    {
        _products.SkuExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var id = await _handler.Handle(new("kb-001", "Keyboard", 49.99m, "USD"), default);

        id.ShouldNotBe(Guid.Empty);
        await _products.Received(1).AddAsync(
            Arg.Is<Product>(p => p.Sku == "KB-001" && p.Price.Amount == 49.99m),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_duplicate_sku_is_rejected_and_nothing_is_saved()
    {
        _products.SkuExistsAsync("KB-001", Arg.Any<CancellationToken>()).Returns(true);

        var ex = await Should.ThrowAsync<ConflictException>(
            () => _handler.Handle(new("kb-001", "Keyboard", 49.99m, "USD"), default));

        ex.Message.ShouldContain("KB-001");
        await _products.DidNotReceive().AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}