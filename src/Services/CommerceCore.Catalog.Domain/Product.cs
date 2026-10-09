using CommerceCore.Catalog.Domain.Exceptions;

namespace CommerceCore.Catalog.Domain;

public sealed class Product
{
    private Product() { }   // for EF Core

    public Guid Id { get; private set; }
    public string Sku { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public Money Price { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static Product Create(Guid id, string sku, string name, Money price, TimeProvider clock)
    {
        if (id == Guid.Empty) throw new DomainException("Product id is required.");
        if (string.IsNullOrWhiteSpace(sku)) throw new DomainException("SKU is required.");
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Name is required.");
        ArgumentNullException.ThrowIfNull(price);
        ArgumentNullException.ThrowIfNull(clock);

        return new Product
        {
            Id = id,
            Sku = sku.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            Price = price,
            IsActive = true,
            CreatedAtUtc = clock.GetUtcNow().UtcDateTime
        };
    }

    public void ChangePrice(Money newPrice)
    {
        ArgumentNullException.ThrowIfNull(newPrice);
        Price = newPrice;
    }

    public void Deactivate() => IsActive = false;
}
