namespace OrderService.Application.DTOs
{
    public record CreateOrderItemRequest(Guid ProductId, int Quantity);
}
