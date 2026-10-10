using System.Net;
using System.Net.Http.Json;
using CommerceCore.Catalog.Domain;
using CommerceCore.Catalog.Infrastructure.Persistence;
using CommerceCore.Contracts;
using CommerceCore.TestSupport;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace CommerceCore.EndToEnd.Tests;

public class PlatformTests(PlatformFixture platform) : IClassFixture<PlatformFixture>
{
    private const string Reserved = "StockReserved";
    private const string Cancelled = "Cancelled";

    private sealed record OrderView(string Status);

    [Fact]
    public async Task Order_is_confirmed_and_stock_is_reserved()
    {
        var product = await CreateProductAsync(stock: 10);

        var orderId = await PlaceOrderAsync(product, quantity: 3);

        (await WaitForOutcomeAsync(orderId)).ShouldBe(Reserved);
        (await StockAsync(product)).ShouldBe((OnHand: 10, Reserved: 3));
    }

    [Fact]
    public async Task Order_is_cancelled_when_stock_is_insufficient()
    {
        var product = await CreateProductAsync(stock: 2);

        var orderId = await PlaceOrderAsync(product, quantity: 3);

        (await WaitForOutcomeAsync(orderId)).ShouldBe(Cancelled);
        (await StockAsync(product)).Reserved.ShouldBe(0);   // all-or-nothing: nothing was reserved
    }

    [Fact]
    public async Task Concurrent_orders_never_oversell()
    {
        var product = await CreateProductAsync(stock: 4);

        var orderIds = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => PlaceOrderAsync(product, quantity: 1)));
        var outcomes = await Task.WhenAll(orderIds.Select(id => WaitForOutcomeAsync(id, TimeSpan.FromSeconds(90))));

        outcomes.Count(o => o == Reserved).ShouldBe(4);
        outcomes.Count(o => o == Cancelled).ShouldBe(2);
        (await StockAsync(product)).Reserved.ShouldBe(4);
    }

    [Fact]
    public async Task Orders_are_accepted_while_the_broker_is_down_and_processed_after_recovery()
    {
        var product = await CreateProductAsync(stock: 5);

        await platform.Broker.StopAsync();
        Guid orderId;
        try
        {
            // The API only needs its own database: the message waits in the outbox table.
            orderId = await PlaceOrderAsync(product, quantity: 1);

            await Task.Delay(TimeSpan.FromSeconds(3));
            (await StatusAsync(orderId)).ShouldBe("Pending");
        }
        finally
        {
            await platform.Broker.StartAsync();
        }

        (await WaitForOutcomeAsync(orderId, TimeSpan.FromSeconds(120))).ShouldBe(Reserved);
    }

    [Fact]
    public async Task A_redelivered_reserve_command_reserves_stock_only_once()
    {
        var product = await CreateProductAsync(stock: 10);
        var orderId = Guid.NewGuid();
        var command = new ReserveStock(orderId, [new OrderItemLine(product, 3)]);

        var bus = platform.CatalogServices.GetRequiredService<IBus>();
        await bus.Publish(command);
        await bus.Publish(command);

        await Eventually.ReadAsync(() => ReservationCountAsync(orderId), count => count >= 1,
            TimeSpan.FromSeconds(60), "the reservation row to appear");
        await Task.Delay(TimeSpan.FromSeconds(3));   // give the duplicate time to be processed too

        (await ReservationCountAsync(orderId)).ShouldBe(1);
        (await StockAsync(product)).Reserved.ShouldBe(3);
    }

    // --- helpers -----------------------------------------------------------------------------

    private async Task<Guid> CreateProductAsync(int stock)
    {
        await using var scope = platform.CatalogServices.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        var product = Product.Create(
            Guid.NewGuid(), "E2E-" + Guid.NewGuid().ToString("N")[..10], "E2E product", new Money(10m), TimeProvider.System);
        db.Products.Add(product);
        db.StockItems.Add(StockItem.Create(product.Id, stock));
        await db.SaveChangesAsync();

        return product.Id;
    }

    private async Task<(int OnHand, int Reserved)> StockAsync(Guid productId)
    {
        await using var scope = platform.CatalogServices.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        var item = await db.StockItems.AsNoTracking().SingleAsync(s => s.ProductId == productId);
        return (item.OnHand, item.Reserved);
    }

    private async Task<int> ReservationCountAsync(Guid orderId)
    {
        await using var scope = platform.CatalogServices.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        return await db.StockReservations.CountAsync(r => r.OrderId == orderId);
    }

    private async Task<Guid> PlaceOrderAsync(Guid productId, int quantity)
    {
        var response = await platform.Orders.PostAsJsonAsync("/api/v1/orders", new
        {
            customerId = Guid.NewGuid(),
            items = new[] { new { productId, quantity } }
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return Guid.Parse(response.Headers.Location!.OriginalString.Split('/')[^1]);
    }

    private async Task<string> StatusAsync(Guid orderId)
    {
        var order = await platform.Orders.GetFromJsonAsync<OrderView>($"/api/v1/orders/{orderId}");
        return order?.Status ?? throw new InvalidOperationException($"Order {orderId} not found.");
    }

    private Task<string> WaitForOutcomeAsync(Guid orderId, TimeSpan? timeout = null) =>
        Eventually.ReadAsync(() => StatusAsync(orderId), s => s is Reserved or Cancelled,
            timeout ?? TimeSpan.FromSeconds(60), $"order {orderId} to reach a final status");
}
