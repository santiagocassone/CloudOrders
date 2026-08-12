namespace CloudOrders.Contracts
{
    public sealed record OrderPlaced(Guid OrderId, IReadOnlyCollection<OrderPlacedItem> Items);
}
