namespace CommerceCore.Ordering.Domain.Events
{
    public class OrderCreatedDomainEvent : IDomainEvent
    {
        public OrderId OrderId { get; }
        public Guid CustomerId { get; }
        public Money TotalAmount { get; }
        public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;

        public OrderCreatedDomainEvent(OrderId orderId, Guid customerId, Money totalAmount)
        {
            OrderId = orderId;
            CustomerId = customerId;
            TotalAmount = totalAmount;
        }
    }
}
