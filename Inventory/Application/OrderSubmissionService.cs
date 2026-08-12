namespace Inventory.Application;

public sealed class OrderSubmissionService(IOrderEventsPublisher publisher)
{
    private readonly Dictionary<Guid, int> _inventory = new()
    {
        [Guid.Parse("11111111-1111-1111-1111-111111111111")] = 10,
        [Guid.Parse("22222222-2222-2222-2222-222222222222")] = 25,
        [Guid.Parse("33333333-3333-3333-3333-333333333333")] = 50
    };

    public Task<int> GetAvailableStockAsync(Guid productId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_inventory.GetValueOrDefault(productId, 0));
    }

    public async Task<(bool Reserved, StockReserved? ReservedEvent, StockRejected? RejectedEvent)> ReserveAsync(Guid orderId, Guid productId, int quantity, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (quantity <= 0)
        {
            var rejected = new StockRejected(orderId, "Quantity must be greater than zero.", DateTime.UtcNow);
            await publisher.PublishAsync(rejected, cancellationToken);
            return (false, null, rejected);
        }

        var available = _inventory.GetValueOrDefault(productId, 0);
        if (available < quantity)
        {
            var rejected = new StockRejected(orderId, $"Insufficient stock for product {productId}.", DateTime.UtcNow);
            await publisher.PublishAsync(rejected, cancellationToken);
            return (false, null, rejected);
        }

        _inventory[productId] = available - quantity;
        var reserved = new StockReserved(orderId, DateTime.UtcNow);
        await publisher.PublishAsync(reserved, cancellationToken);
        return (true, reserved, null);
    }
}
