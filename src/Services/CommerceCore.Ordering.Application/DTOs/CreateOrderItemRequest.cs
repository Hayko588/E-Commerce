namespace CommerceCore.Ordering.Application.DTOs
{
    public record CreateOrderItemRequest(Guid ProductId, int Quantity);
}
