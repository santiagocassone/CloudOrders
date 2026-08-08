namespace CloudOrders.Api.Contracts;

public sealed record CreateOrderRequest(Guid CustomerId, decimal Total);