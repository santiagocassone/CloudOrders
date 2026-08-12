namespace CloudOrders.Application.Orders;

public sealed record StockResult(Guid OrderId, StockResultStatus Status, string? Reason);