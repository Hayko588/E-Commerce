using OrderService.Domain.Entities;
using OrderService.Domain.Enums;
using OrderService.Domain.Events;
using OrderService.Domain.Exceptions;

namespace OrderService.Domain
{
    public class Order
    {
        private readonly List<OrderItem> _items = new();
        private readonly List<IDomainEvent> _domainEvents = new();

        public OrderId Id { get; private set; }
        public Guid CustomerId { get; private set; }
        public OrderStatus Status { get; private set; }
        public DateTime CreatedAtUtc { get; private set; }
        public DateTime? UpdatedAtUtc { get; private set; }

        public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        public Money TotalAmount => _items.Count == 0
            ? Money.Zero()
            : _items.Select(i => i.TotalPrice).Aggregate((a, b) => a + b);

        private Order() { }

        public Order(Guid customerId, params ReadOnlySpan<OrderItem> items)
        {
            if (customerId == Guid.Empty)
                throw new ArgumentException("Customer ID cannot be empty.", nameof(customerId));

            if (items.IsEmpty)
                throw new ArgumentException("Order must contain at least one item.", nameof(items));

            Id = OrderId.New();
            CustomerId = customerId;
            _items.AddRange(items);
            Status = OrderStatus.Pending;
            CreatedAtUtc = DateTime.UtcNow;

            AddDomainEvent(new OrderCreatedDomainEvent(Id, CustomerId, TotalAmount));
        }

        private void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
        public void ClearDomainEvents() => _domainEvents.Clear();

        private void EnsureStatus(OrderStatus expected, string action)
        {
            if (Status != expected)
                throw new DomainException($"Cannot {action} order in state '{Status}'.");
        }
    }
}
