using OrderService.Domain.Exceptions;

namespace OrderService.Domain
{
    public readonly record struct OrderId(Guid Value)
    {
        public static OrderId New() => new(Guid.NewGuid());
        public static OrderId Empty => new(Guid.Empty);
    }

    public readonly record struct ProductId(Guid Value)
    {
        public static ProductId New() => new(Guid.NewGuid());
    }

    public record Money
    {
        public decimal Amount { get; }
        public string Currency { get; }

        public Money(decimal amount, string currency = "USD")
        {
            ArgumentOutOfRangeException.ThrowIfNegative(amount);
            ArgumentException.ThrowIfNullOrWhiteSpace(currency);

            // Standard financial rounding to 2 decimal places
            Amount = decimal.Round(amount, 2, MidpointRounding.ToEven);
            Currency = currency.ToUpperInvariant();
        }

        public static Money Zero(string currency = "USD") => new(0, currency);

        // Operator Overloads
        public static Money operator +(Money left, Money right)
        {
            EnsureSameCurrency(left, right);
            return new Money(left.Amount + right.Amount, left.Currency);
        }

        public static Money operator -(Money left, Money right)
        {
            EnsureSameCurrency(left, right);
            return new Money(left.Amount - right.Amount, left.Currency);
        }

        public static Money operator *(Money money, decimal multiplier) =>
            new(money.Amount * multiplier, money.Currency);

        private static void EnsureSameCurrency(Money left, Money right)
        {
            if (left.Currency != right.Currency)
            {
                throw new DomainException(
                    $"Cannot operate on different currencies: '{left.Currency}' and '{right.Currency}'.");
            }
        }
    }
}
