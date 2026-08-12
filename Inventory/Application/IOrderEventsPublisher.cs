namespace Inventory.Application;

public interface IOrderEventsPublisher
{
    Task PublishAsync(StockReserved reserved, CancellationToken cancellationToken);
    Task PublishAsync(StockRejected rejected, CancellationToken cancellationToken);
}
