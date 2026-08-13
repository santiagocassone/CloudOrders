using Inventory.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application;

public sealed class OrderSubmissionService(InventoryDbContext dbContext, IOrderEventsPublisher publisher)
{
    public async Task<InventoryItem> GetStockAsync(Guid productId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var item = await dbContext.InventoryItems
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.ProductId == productId, cancellationToken);

        return item ?? new InventoryItem
        {
            ProductId = productId,
            AvailableQuantity = 0,
            ReservedQuantity = 0,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public async Task<int> GetAvailableStockAsync(Guid productId, CancellationToken cancellationToken)
    {
        var stock = await GetStockAsync(productId, cancellationToken);
        return stock.AvailableQuantity;
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

        var item = await dbContext.InventoryItems
            .SingleOrDefaultAsync(x => x.ProductId == productId, cancellationToken);

        var available = item?.AvailableQuantity ?? 0;
        if (available < quantity)
        {
            var rejected = new StockRejected(orderId, $"Insufficient stock for product {productId}.", DateTime.UtcNow);
            await publisher.PublishAsync(rejected, cancellationToken);
            return (false, null, rejected);
        }

        item!.AvailableQuantity -= quantity;
        item.ReservedQuantity += quantity;
        item.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        var reserved = new StockReserved(orderId, DateTime.UtcNow);
        await publisher.PublishAsync(reserved, cancellationToken);
        return (true, reserved, null);
    }
}
