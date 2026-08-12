namespace CloudOrders.Contracts
{
    public sealed record OrderPlacedItem(Guid ProductId, int Quantity);
}
