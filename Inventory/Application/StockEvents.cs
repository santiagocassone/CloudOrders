namespace Inventory.Application;

public sealed record OrderPlaced(
    Guid OrderId,
    Guid CustomerId,
    decimal Total,
    IReadOnlyList<OrderLine> Lines,
    DateTime OccurredAt);

public sealed record OrderLine(Guid ProductId, int Quantity);

public sealed record StockReserved(Guid OrderId, DateTime OccurredAt);

public sealed record StockRejected(Guid OrderId, string Reason, DateTime OccurredAt);
