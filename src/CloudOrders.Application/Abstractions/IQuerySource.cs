using CloudOrders.Domain;

namespace CloudOrders.Application.Abstractions;

public interface IQuerySource
{
    Task<Order?> GetOrderByIdAsync(Guid id, CancellationToken cancellationToken);
}