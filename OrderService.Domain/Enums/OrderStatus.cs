namespace OrderService.Domain.Enums
{
    public enum OrderStatus
    {
        Pending,
        StockReserved,
        Paid,
        Completed,
        Cancelled,
        Failed
    }
}
