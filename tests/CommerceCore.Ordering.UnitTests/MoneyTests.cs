using CommerceCore.Ordering.Domain;
using CommerceCore.Ordering.Domain.Exceptions;
using Shouldly;
using Xunit;

namespace CommerceCore.Ordering.UnitTests;

public class MoneyTests
{
    public static TheoryData<decimal, decimal> Rounding => new()
    {
        { 1.005m, 1.00m },
        { 1.015m, 1.02m },
        { 0.125m, 0.12m },
    };

    [Theory, MemberData(nameof(Rounding))]
    public void Amount_is_rounded_to_two_decimals_using_bankers_rounding(decimal input, decimal expected) =>
        new Money(input).Amount.ShouldBe(expected);

    [Fact]
    public void Negative_amounts_are_rejected() =>
        Should.Throw<ArgumentOutOfRangeException>(() => new Money(-0.01m));

    [Fact]
    public void Currency_is_normalised_to_upper_case() =>
        new Money(5, "usd").Currency.ShouldBe("USD");

    [Fact]
    public void Adding_different_currencies_throws_a_DomainException() =>
        Should.Throw<DomainException>(() => new Money(1, "USD") + new Money(1, "EUR"));

    [Fact]
    public void Addition_and_multiplication_work()
    {
        (new Money(1.10m) + new Money(2.20m)).ShouldBe(new Money(3.30m));
        (new Money(10.10m) * 3).ShouldBe(new Money(30.30m));
    }

    [Fact]
    public void Equality_is_by_value() =>
        new Money(5, "USD").ShouldBe(new Money(5, "usd"));
}