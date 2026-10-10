using CommerceCore.Catalog.Domain.Exceptions;

namespace CommerceCore.Catalog.Domain;

public sealed class StockReservation
{
    private StockReservation() { }   // for EF Core

    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static StockReservation Create(Guid orderId, Guid productId, int quantity, DateTime createdAtUtc)
    {
        if (quantity <= 0) throw new DomainException("Reserved quantity must be positive.");

        return new StockReservation
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            ProductId = productId,
            Quantity = quantity,
            CreatedAtUtc = createdAtUtc
        };
    }
}
