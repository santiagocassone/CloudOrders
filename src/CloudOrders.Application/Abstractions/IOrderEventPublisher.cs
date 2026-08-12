using CloudOrders.Contracts;

namespace CloudOrders.Application.Abstractions;

public interface IOrderEventPublisher
{
    Task PublishOrderPlacedAsync(OrderPlaced message, CancellationToken cancellationToken);
}