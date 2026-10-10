using CommerceCore.Ordering.Domain;
using CommerceCore.Ordering.Domain.Entities;
using CommerceCore.Ordering.Domain.Enums;
using CommerceCore.Ordering.Domain.Events;
using CommerceCore.Ordering.Domain.Exceptions;
using Shouldly;

namespace CommerceCore.Ordering.UnitTests;

public class OrderTests
{
    private static OrderItem Item(decimal price, int quantity = 1, string currency = "USD") =>
        new(ProductId.New(), new Money(price, currency), quantity);

    [Fact]
    public void New_order_is_pending_and_totals_its_items()
    {
        var order = new Order(Guid.NewGuid(), Item(10.50m, 2), Item(5m));

        order.Status.ShouldBe(OrderStatus.Pending);
        order.Items.Count.ShouldBe(2);
        order.TotalAmount.ShouldBe(new Money(26m));
    }

    [Fact]
    public void New_order_raises_an_OrderCreated_event()
    {
        var customerId = Guid.NewGuid();
        var order = new Order(customerId, Item(10m));

        var evt = order.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<OrderCreatedDomainEvent>();

        evt.OrderId.ShouldBe(order.Id);
        evt.CustomerId.ShouldBe(customerId);
        evt.TotalAmount.ShouldBe(new Money(10m));
    }

    [Fact]
    public void Order_requires_a_customer() =>
        Should.Throw<ArgumentException>(() => new Order(Guid.Empty, Item(1m)));

    [Fact]
    public void Order_requires_at_least_one_item() =>
        Should.Throw<ArgumentException>(() => new Order(Guid.NewGuid()));

    [Fact]
    public void Mixed_currencies_are_rejected() =>
        Should.Throw<DomainException>(() =>
            new Order(Guid.NewGuid(), Item(10m), Item(10m, currency: "EUR")));

    [Fact]
    public void Reserved_stock_moves_a_pending_order_forward()
    {
        var order = new Order(Guid.NewGuid(), Item(10m));

        order.MarkStockReserved();

        order.Status.ShouldBe(OrderStatus.StockReserved);
        order.UpdatedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void Stock_cannot_be_reserved_twice()
    {
        var order = new Order(Guid.NewGuid(), Item(10m));
        order.MarkStockReserved();

        Should.Throw<DomainException>(order.MarkStockReserved);
    }

    [Fact]
    public void A_pending_order_can_be_cancelled_but_not_twice()
    {
        var order = new Order(Guid.NewGuid(), Item(10m));

        order.Cancel();

        order.Status.ShouldBe(OrderStatus.Cancelled);
        Should.Throw<DomainException>(order.Cancel);
    }
}
