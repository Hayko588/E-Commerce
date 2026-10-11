namespace CommerceCore.Contracts;

public sealed record OrderItemLine(Guid ProductId, int Quantity);

// Ordering -> saga (published through the outbox when an order is created)
public sealed record OrderSubmitted(
    Guid OrderId, Guid CustomerId, decimal Total, string Currency, string PaymentToken,
    IReadOnlyList<OrderItemLine> Items);

// saga -> Catalog
public sealed record ReserveStock(Guid OrderId, IReadOnlyList<OrderItemLine> Items);
public sealed record ReleaseStock(Guid OrderId);                       // compensation

// Catalog -> saga
public sealed record StockReserved(Guid OrderId);
public sealed record StockReservationRejected(Guid OrderId, string Reason);
public sealed record StockReleased(Guid OrderId);

// saga -> Payments
public sealed record ChargePayment(Guid OrderId, decimal Amount, string Currency, string PaymentToken);

// Payments -> saga
public sealed record PaymentSucceeded(Guid OrderId, Guid PaymentId);
public sealed record PaymentFailed(Guid OrderId, string Reason);

// saga -> Ordering (updates the Order aggregate)
public sealed record OrderStockReserved(Guid OrderId);
public sealed record OrderConfirmed(Guid OrderId, Guid CustomerId);
public sealed record OrderRejected(Guid OrderId, Guid CustomerId, string Reason);
