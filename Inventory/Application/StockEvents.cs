namespace Inventory.Application;

public sealed record OrderPlaced(Guid OrderId, IReadOnlyCollection<OrderPlacedItem> Items);

public sealed record OrderPlacedItem(Guid ProductId, int Quantity);

public sealed record StockReserved(Guid OrderId, DateTime OccurredAt);

public sealed record StockRejected(Guid OrderId, string Reason, DateTime OccurredAt);
