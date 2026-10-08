namespace CommerceCore.Ordering.Domain.Entities
{
    public class OrderItem
    {
        public Guid Id { get; private set; }
        public ProductId ProductId { get; private set; }
        public Money UnitPrice { get; private set; }
        public int Quantity { get; private set; }
        public Money TotalPrice => UnitPrice * Quantity;

        private OrderItem() { }

        public OrderItem(ProductId productId, Money unitPrice, int quantity)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
            ArgumentNullException.ThrowIfNull(unitPrice);

            Id = Guid.NewGuid();
            ProductId = productId;
            UnitPrice = unitPrice;
            Quantity = quantity;
        }
    }
}
