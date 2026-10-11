using CommerceCore.Catalog.Domain.Exceptions;

namespace CommerceCore.Catalog.Domain;

public sealed class StockItem
{
    private StockItem() { }   // for EF Core

    public Guid ProductId { get; private set; }
    public int OnHand { get; private set; }
    public int Reserved { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public int Available => OnHand - Reserved;

    public static StockItem Create(Guid productId, int onHand)
    {
        if (productId == Guid.Empty) throw new DomainException("Product id is required.");
        if (onHand < 0) throw new DomainException("Stock on hand cannot be negative.");

        return new StockItem { ProductId = productId, OnHand = onHand };
    }

    public bool CanReserve(int quantity) => quantity > 0 && Available >= quantity;

    public void Reserve(int quantity)
    {
        if (!CanReserve(quantity))
            throw new DomainException($"Cannot reserve {quantity}; only {Available} available.");

        Reserved += quantity;
    }

    public void Release(int quantity)
    {
        if (quantity <= 0 || quantity > Reserved)
            throw new DomainException($"Cannot release {quantity}; only {Reserved} reserved.");

        Reserved -= quantity;
    }
}
