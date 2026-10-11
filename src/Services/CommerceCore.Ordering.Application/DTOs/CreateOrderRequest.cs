using CommerceCore.Ordering.Application.DTOs;

public record CreateOrderRequest(
    Guid CustomerId,
    List<CreateOrderItemRequest> Items,
    string PaymentToken = "tok_ok"
);
