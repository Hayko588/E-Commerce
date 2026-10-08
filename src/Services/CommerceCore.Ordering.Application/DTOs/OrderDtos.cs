namespace CommerceCore.Ordering.Application.DTOs
{
    public record CreateOrderRequest(
        Guid CustomerId,
        List<CreateOrderItemRequest> Items
    );

    public record OrderItemResponse(
        Guid ProductId,
        int Quantity,
        decimal UnitPrice,
        decimal TotalPrice
    );

    public record OrderResponse(
        Guid OrderId,
        Guid CustomerId,
        string Status,
        decimal TotalAmount,
        string Currency,
        List<OrderItemResponse> Items,
        DateTime CreatedAtUtc
    );
}