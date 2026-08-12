using System.Collections.Concurrent;
using Inventory.Application;

namespace Inventory.Infrastructure;

public sealed class InMemoryOrderEventsPublisher : IOrderEventsPublisher
{
    private readonly ConcurrentQueue<object> _published = new();

    public IReadOnlyCollection<object> Published => _published.ToArray();

    public Task PublishAsync(StockReserved reserved, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _published.Enqueue(reserved);
        return Task.CompletedTask;
    }

    public Task PublishAsync(StockRejected rejected, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _published.Enqueue(rejected);
        return Task.CompletedTask;
    }
}
