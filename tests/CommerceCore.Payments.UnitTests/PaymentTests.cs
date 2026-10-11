using CommerceCore.Payments.Application;
using CommerceCore.Payments.Domain;
using CommerceCore.Payments.Infrastructure.Gateway;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CommerceCore.Payments.UnitTests;

public class PaymentTests
{
    private static readonly DateTime Now = new(2026, 10, 12, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void A_successful_payment_is_normalised_and_carries_the_provider_reference()
    {
        var payment = Payment.Succeeded(Guid.NewGuid(), 10.005m, "usd", "sim_abc", Now);

        payment.Status.ShouldBe(PaymentStatus.Succeeded);
        payment.Amount.ShouldBe(10.00m);          // banker's rounding
        payment.Currency.ShouldBe("USD");
        payment.ProviderReference.ShouldBe("sim_abc");
        payment.FailureReason.ShouldBeNull();
    }

    [Fact]
    public void A_failed_payment_carries_the_reason()
    {
        var payment = Payment.Failed(Guid.NewGuid(), 25m, "USD", "Card declined.", Now);

        payment.Status.ShouldBe(PaymentStatus.Failed);
        payment.FailureReason.ShouldBe("Card declined.");
        payment.ProviderReference.ShouldBeNull();
    }

    [Fact]
    public void An_order_id_is_required() =>
        Should.Throw<DomainException>(() => Payment.Succeeded(Guid.Empty, 10m, "USD", "ref", Now));

    [Fact]
    public void A_negative_amount_is_rejected() =>
        Should.Throw<DomainException>(() => Payment.Succeeded(Guid.NewGuid(), -1m, "USD", "ref", Now));

    [Theory]
    [InlineData("")]
    [InlineData("US")]
    [InlineData("USDX")]
    public void The_currency_must_have_three_letters(string currency) =>
        Should.Throw<DomainException>(() => Payment.Succeeded(Guid.NewGuid(), 10m, currency, "ref", Now));
}

public class SimulatedPaymentGatewayTests
{
    private readonly SimulatedPaymentGateway _gateway = new();

    [Fact]
    public async Task The_ok_token_is_approved_with_a_reference()
    {
        var result = await _gateway.ChargeAsync("0123456789abcdef", 10m, "USD", "tok_ok");

        result.Approved.ShouldBeTrue();
        result.Reference.ShouldNotBeNull().ShouldStartWith("sim_");
    }

    [Theory]
    [InlineData("tok_decline", "Card declined.")]
    [InlineData("tok_insufficient_funds", "Insufficient funds.")]
    [InlineData("anything-else", "Unknown payment token.")]
    public async Task Other_tokens_are_declined_with_a_reason(string token, string reason)
    {
        var result = await _gateway.ChargeAsync("0123456789abcdef", 10m, "USD", token);

        result.Approved.ShouldBeFalse();
        result.DeclineReason.ShouldBe(reason);
    }
}

public class PaymentProcessorTests
{
    private readonly IPaymentRepository _payments = Substitute.For<IPaymentRepository>();
    private readonly IPaymentGateway _gateway = Substitute.For<IPaymentGateway>();
    private readonly PaymentProcessor _processor;

    public PaymentProcessorTests() => _processor = new(_payments, _gateway, TimeProvider.System);

    [Fact]
    public async Task An_approved_charge_is_recorded_and_uses_the_order_id_as_idempotency_key()
    {
        var orderId = Guid.NewGuid();
        _gateway.ChargeAsync(Arg.Any<string>(), Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(GatewayResult.Approve("sim_1"));

        var payment = await _processor.ChargeAsync(orderId, 25m, "USD", "tok_ok");

        payment.Status.ShouldBe(PaymentStatus.Succeeded);
        _payments.Received(1).Add(Arg.Is<Payment>(p => p.OrderId == orderId && p.Status == PaymentStatus.Succeeded));
        await _gateway.Received(1).ChargeAsync(orderId.ToString("N"), 25m, "USD", "tok_ok", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_declined_charge_is_recorded_as_failed_with_the_reason()
    {
        _gateway.ChargeAsync(Arg.Any<string>(), Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(GatewayResult.Decline("Card declined."));

        var payment = await _processor.ChargeAsync(Guid.NewGuid(), 25m, "USD", "tok_decline");

        payment.Status.ShouldBe(PaymentStatus.Failed);
        payment.FailureReason.ShouldBe("Card declined.");
        _payments.Received(1).Add(Arg.Is<Payment>(p => p.Status == PaymentStatus.Failed));
    }

    [Fact]
    public async Task A_second_request_for_the_same_order_returns_the_original_outcome_without_charging_again()
    {
        var orderId = Guid.NewGuid();
        var original = Payment.Succeeded(orderId, 25m, "USD", "sim_1", DateTime.UtcNow);
        _payments.GetByOrderIdAsync(orderId, Arg.Any<CancellationToken>()).Returns(original);

        var payment = await _processor.ChargeAsync(orderId, 25m, "USD", "tok_ok");

        payment.ShouldBeSameAs(original);
        await _gateway.DidNotReceiveWithAnyArgs()
            .ChargeAsync(default!, default, default!, default!, default);
        _payments.DidNotReceiveWithAnyArgs().Add(default!);
    }
}
