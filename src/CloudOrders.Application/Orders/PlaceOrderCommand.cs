namespace CloudOrders.Application.Orders;

public sealed record PlaceOrderCommand(Guid CustomerId, IReadOnlyCollection<PlaceOrderItem> Items);