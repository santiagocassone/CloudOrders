namespace CloudOrders.Application.Orders
{
    public sealed class OrderNotFoundException : Exception
    {
        public OrderNotFoundException(Guid orderId) : base($"Order not found. OrderId: {orderId}") { }
        public OrderNotFoundException(Guid orderId, Exception ex) : base($"Order not found. OrderId: {orderId}", ex) { }
    }
}
