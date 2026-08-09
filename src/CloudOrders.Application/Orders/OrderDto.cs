namespace CloudOrders.Application.Orders;

public sealed record OrderDto(Guid Id, Guid CustomerId, decimal Total, string Status, DateTime CreatedAt);