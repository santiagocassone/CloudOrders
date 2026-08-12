namespace CloudOrders.Domain
{
    public class OrderItem
    {
        public Guid ProductId { get; private set; }
        public int Quantity { get; private set; }
        public decimal UnitPrice { get; private set; }
        public decimal Subtotal => Quantity * UnitPrice;
        private OrderItem() { }
        public static OrderItem Create(Guid productId, int quantity, decimal unitPrice)
        {
            if (productId == Guid.Empty)
            {
                throw new ArgumentException("Product ID cannot be empty", nameof(productId));
            }
            if (quantity <= 0)
            {
                throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));
            }
            if (unitPrice <= 0)
            {
                throw new ArgumentException("Unit price must be greater than zero", nameof(unitPrice));
            }
            return new OrderItem()
            {
                ProductId = productId,
                Quantity = quantity,
                UnitPrice = unitPrice
            };
        }
    }
}
