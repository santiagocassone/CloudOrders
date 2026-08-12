namespace CloudOrders.Application.Orders
{
    public sealed record PlaceOrderItem(Guid ProductId, int Quantity, decimal UnitPrice);
}
