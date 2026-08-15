namespace CloudOrders.Contracts;

public sealed record StockRejected(Guid OrderId, string Reason, DateTime RejectedAt);