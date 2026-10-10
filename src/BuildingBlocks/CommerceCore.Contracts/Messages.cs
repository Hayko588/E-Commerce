namespace CommerceCore.Contracts;

public sealed record OrderItemLine(Guid ProductId, int Quantity);

// Ordering -> saga (published through the outbox when an order is created)
public sealed record OrderSubmitted(Guid OrderId, Guid CustomerId, IReadOnlyList<OrderItemLine> Items);

// Saga -> Catalog
public sealed record ReserveStock(Guid OrderId, IReadOnlyList<OrderItemLine> Items);

// Catalog -> saga
public sealed record StockReserved(Guid OrderId);
public sealed record StockReservationRejected(Guid OrderId, string Reason);

// Saga -> Ordering (updates the Order aggregate)
public sealed record OrderConfirmed(Guid OrderId);
public sealed record OrderRejected(Guid OrderId, string Reason);
