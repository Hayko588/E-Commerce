using CommerceCore.Catalog.Domain.Exceptions;

namespace CommerceCore.Catalog.Domain;

public record Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency = "USD")
    {
        if (amount < 0)
            throw new DomainException("Amount cannot be negative.");
        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)
            throw new DomainException("Currency must be a 3-letter ISO 4217 code.");

        Amount = decimal.Round(amount, 2, MidpointRounding.ToEven);
        Currency = currency.Trim().ToUpperInvariant();
    }
}