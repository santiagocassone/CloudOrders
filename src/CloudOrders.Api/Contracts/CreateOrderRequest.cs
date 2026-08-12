namespace CloudOrders.Api.Contracts;

public sealed record CreateOrderRequest(Guid CustomerId, IReadOnlyCollection<CreateOrderItemRequest> Items);