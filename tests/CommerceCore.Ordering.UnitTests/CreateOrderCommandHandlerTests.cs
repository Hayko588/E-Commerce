using CommerceCore.Ordering.Application;
using CommerceCore.Ordering.Application.Commands;
using CommerceCore.Ordering.Application.DTOs;
using CommerceCore.Ordering.Domain;
using CommerceCore.Ordering.Domain.Exceptions;
using NSubstitute;
using Shouldly;

namespace CommerceCore.Ordering.UnitTests;

public class CreateOrderCommandHandlerTests
{
    private readonly IOrderRepository _orders = Substitute.For<IOrderRepository>();
    private readonly IProductCatalogClient _catalog = Substitute.For<IProductCatalogClient>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CreateOrderCommandHandler _handler;

    public CreateOrderCommandHandlerTests() => _handler = new(_orders, _catalog, _unitOfWork);

    private void CatalogReturns(params (Guid Id, decimal Price)[] prices)
    {
        IReadOnlyDictionary<Guid, Money> result = prices.ToDictionary(p => p.Id, p => new Money(p.Price));
        _catalog.GetPricesAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(result);
    }

    [Fact]
    public async Task Order_is_priced_from_the_catalog_and_saved()
    {
        var keyboard = Guid.NewGuid();
        CatalogReturns((keyboard, 49.99m));
        var command = new CreateOrderCommand(Guid.NewGuid(), [new CreateOrderItemRequest(keyboard, 2)]);

        var orderId = await _handler.Handle(command, default);

        orderId.ShouldNotBe(Guid.Empty);
        await _orders.Received(1).AddAsync(
            Arg.Is<Order>(o => o.TotalAmount.Amount == 99.98m),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Unknown_product_is_rejected_and_nothing_is_saved()
    {
        var unknown = Guid.NewGuid();
        CatalogReturns();
        var command = new CreateOrderCommand(Guid.NewGuid(), [new CreateOrderItemRequest(unknown, 1)]);

        var ex = await Should.ThrowAsync<DomainException>(() => _handler.Handle(command, default));

        ex.Message.ShouldContain(unknown.ToString());
        await _orders.DidNotReceive().AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}