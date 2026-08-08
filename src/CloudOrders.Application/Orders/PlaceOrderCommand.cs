namespace CloudOrders.Application.Orders;

public sealed record PlaceOrderCommand(Guid CustomerId, decimal Total);