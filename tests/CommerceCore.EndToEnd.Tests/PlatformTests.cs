using System.Net;
using System.Net.Http.Json;
using CommerceCore.Catalog.Domain;
using CommerceCore.Catalog.Infrastructure.Persistence;
using CommerceCore.Contracts;
using CommerceCore.Payments.Infrastructure.Persistence;
using CommerceCore.TestSupport;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace CommerceCore.EndToEnd.Tests;

public class PlatformTests(PlatformFixture platform) : IClassFixture<PlatformFixture>
{
    private const string Paid = "Paid";
    private const string Cancelled = "Cancelled";

    private sealed record OrderView(string Status);
    private sealed record PaymentView(string Status, string? FailureReason);

    [Fact]
    public async Task Order_is_paid_and_stock_is_reserved()
    {
        var product = await CreateProductAsync(stock: 10);

        var orderId = await PlaceOrderAsync(product, quantity: 3);

        (await WaitForOutcomeAsync(orderId)).ShouldBe(Paid);
        (await StockAsync(product)).ShouldBe((OnHand: 10, Reserved: 3));
        (await PaymentAsync(orderId)).ShouldNotBeNull().Status.ShouldBe("Succeeded");
    }

    [Fact]
    public async Task Order_is_cancelled_when_stock_is_insufficient_and_is_never_charged()
    {
        var product = await CreateProductAsync(stock: 2);

        var orderId = await PlaceOrderAsync(product, quantity: 3);

        (await WaitForOutcomeAsync(orderId)).ShouldBe(Cancelled);
        (await StockAsync(product)).Reserved.ShouldBe(0);   // all-or-nothing: nothing was reserved
        (await PaymentAsync(orderId)).ShouldBeNull();       // payment was never attempted
    }

    [Fact]
    public async Task Declined_payment_cancels_the_order_and_releases_the_stock()
    {
        var product = await CreateProductAsync(stock: 10);

        var orderId = await PlaceOrderAsync(product, quantity: 4, paymentToken: "tok_decline");

        (await WaitForOutcomeAsync(orderId)).ShouldBe(Cancelled);

        // The compensation ran before the order was cancelled, so all of this is already true.
        (await StockAsync(product)).Reserved.ShouldBe(0);
        (await ReservationsAsync(orderId)).ShouldAllBe(r => r.ReleasedAtUtc != null);

        var payment = (await PaymentAsync(orderId)).ShouldNotBeNull();
        payment.Status.ShouldBe("Failed");
        payment.FailureReason.ShouldBe("Card declined.");
    }

    [Fact]
    public async Task Concurrent_orders_never_oversell()
    {
        var product = await CreateProductAsync(stock: 4);

        var orderIds = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => PlaceOrderAsync(product, quantity: 1)));
        var outcomes = await Task.WhenAll(orderIds.Select(id => WaitForOutcomeAsync(id, TimeSpan.FromSeconds(90))));

        outcomes.Count(o => o == Paid).ShouldBe(4);
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

        (await WaitForOutcomeAsync(orderId, TimeSpan.FromSeconds(120))).ShouldBe(Paid);
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

    [Fact]
    public async Task A_redelivered_charge_command_charges_only_once()
    {
        var orderId = Guid.NewGuid();
        var command = new ChargePayment(orderId, 25m, "USD", "tok_ok");

        var bus = platform.PaymentsServices.GetRequiredService<IBus>();
        await bus.Publish(command);
        await bus.Publish(command);

        await Eventually.ReadAsync(() => PaymentCountAsync(orderId), count => count >= 1,
            TimeSpan.FromSeconds(60), "the payment row to appear");
        await Task.Delay(TimeSpan.FromSeconds(3));

        (await PaymentCountAsync(orderId)).ShouldBe(1);   // the unique index plus the retry keep it at one
    }

    [Fact]
    public async Task A_redelivered_release_command_releases_stock_only_once()
    {
        var product = await CreateProductAsync(stock: 10);
        var orderId = Guid.NewGuid();
        var bus = platform.CatalogServices.GetRequiredService<IBus>();

        await bus.Publish(new ReserveStock(orderId, [new OrderItemLine(product, 4)]));
        await Eventually.ReadAsync(() => StockAsync(product), s => s.Reserved == 4,
            TimeSpan.FromSeconds(60), "the stock to be reserved");

        var release = new ReleaseStock(orderId);
        await bus.Publish(release);
        await bus.Publish(release);

        await Eventually.ReadAsync(() => StockAsync(product), s => s.Reserved == 0,
            TimeSpan.FromSeconds(60), "the stock to be released");
        await Task.Delay(TimeSpan.FromSeconds(3));

        // A second release must not push Reserved below zero (the database constraint would reject it).
        (await StockAsync(product)).ShouldBe((OnHand: 10, Reserved: 0));
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

    private async Task<List<StockReservation>> ReservationsAsync(Guid orderId)
    {
        await using var scope = platform.CatalogServices.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        return await db.StockReservations.AsNoTracking().Where(r => r.OrderId == orderId).ToListAsync();
    }

    private async Task<int> PaymentCountAsync(Guid orderId)
    {
        await using var scope = platform.PaymentsServices.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();

        return await db.Payments.CountAsync(p => p.OrderId == orderId);
    }

    private async Task<PaymentView?> PaymentAsync(Guid orderId)
    {
        var response = await platform.Payments.GetAsync($"/api/v1/payments?orderId={orderId}");
        if (response.StatusCode == HttpStatusCode.NotFound) return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PaymentView>();
    }

    private async Task<Guid> PlaceOrderAsync(Guid productId, int quantity, string paymentToken = "tok_ok")
    {
        var response = await platform.Orders.PostAsJsonAsync("/api/v1/orders", new
        {
            customerId = Guid.NewGuid(),
            paymentToken,
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
        Eventually.ReadAsync(() => StatusAsync(orderId), s => s is Paid or Cancelled,
            timeout ?? TimeSpan.FromSeconds(60), $"order {orderId} to reach a final status");
}
