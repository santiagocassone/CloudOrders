namespace CloudOrders.Contracts;

public sealed record StockReserved(Guid OrderId, DateTime ReservedAt);