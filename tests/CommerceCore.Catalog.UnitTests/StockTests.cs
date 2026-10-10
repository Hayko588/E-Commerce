using CommerceCore.Catalog.Application.Stock;
using CommerceCore.Catalog.Domain;
using CommerceCore.Catalog.Domain.Exceptions;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CommerceCore.Catalog.UnitTests;

public class StockItemTests
{
    private static readonly Guid Product = Guid.NewGuid();

    [Fact]
    public void Reserving_reduces_the_available_quantity()
    {
        var stock = StockItem.Create(Product, 10);

        stock.Reserve(3);

        stock.Reserved.ShouldBe(3);
        stock.Available.ShouldBe(7);
    }

    [Fact]
    public void Reserving_more_than_available_is_rejected() =>
        Should.Throw<DomainException>(() => StockItem.Create(Product, 2).Reserve(3));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Quantity_must_be_positive(int quantity) =>
        Should.Throw<DomainException>(() => StockItem.Create(Product, 10).Reserve(quantity));

    [Fact]
    public void Stock_cannot_start_negative() =>
        Should.Throw<DomainException>(() => StockItem.Create(Product, -1));
}

public class StockReservationServiceTests
{
    private static readonly Guid Keyboard = Guid.NewGuid();
    private static readonly Guid Mouse = Guid.NewGuid();

    private readonly IStockRepository _stock = Substitute.For<IStockRepository>();
    private readonly StockReservationService _service;

    public StockReservationServiceTests() => _service = new(_stock, TimeProvider.System);

    private void StockIs(params (Guid Id, int OnHand)[] levels) =>
        _stock.GetStockAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(levels.ToDictionary(l => l.Id, l => StockItem.Create(l.Id, l.OnHand)));

    [Fact]
    public async Task Sufficient_stock_is_reserved_for_every_line()
    {
        StockIs((Keyboard, 10), (Mouse, 10));

        var outcome = await _service.ReserveAsync(Guid.NewGuid(),
            [new(Keyboard, 2), new(Mouse, 1)]);

        outcome.Reserved.ShouldBeTrue();
        _stock.Received(2).AddReservation(Arg.Any<StockReservation>());
    }

    [Fact]
    public async Task One_short_line_rejects_the_whole_order_and_reserves_nothing()
    {
        StockIs((Keyboard, 10), (Mouse, 1));

        var outcome = await _service.ReserveAsync(Guid.NewGuid(),
            [new(Keyboard, 2), new(Mouse, 5)]);

        outcome.Reserved.ShouldBeFalse();
        outcome.RejectionReason.ShouldNotBeNull().ShouldContain("available 1");
        _stock.DidNotReceive().AddReservation(Arg.Any<StockReservation>());
    }

    [Fact]
    public async Task Unknown_product_is_rejected()
    {
        StockIs();

        var outcome = await _service.ReserveAsync(Guid.NewGuid(), [new(Keyboard, 1)]);

        outcome.Reserved.ShouldBeFalse();
    }

    [Fact]
    public async Task Redelivery_of_an_already_reserved_order_succeeds_without_reserving_again()
    {
        var orderId = Guid.NewGuid();
        _stock.HasReservationAsync(orderId, Arg.Any<CancellationToken>()).Returns(true);

        var outcome = await _service.ReserveAsync(orderId, [new(Keyboard, 1)]);

        outcome.Reserved.ShouldBeTrue();
        _stock.DidNotReceive().AddReservation(Arg.Any<StockReservation>());
        await _stock.DidNotReceive().GetStockAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Duplicate_lines_for_one_product_are_summed()
    {
        StockIs((Keyboard, 3));

        var outcome = await _service.ReserveAsync(Guid.NewGuid(), [new(Keyboard, 2), new(Keyboard, 2)]);

        outcome.Reserved.ShouldBeFalse();   // 4 requested, 3 available
    }
}
