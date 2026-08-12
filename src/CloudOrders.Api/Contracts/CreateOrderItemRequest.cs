namespace CloudOrders.Api.Contracts
{
    public sealed record CreateOrderItemRequest(Guid ProductId, int Quantity, decimal UnitPrice);
}