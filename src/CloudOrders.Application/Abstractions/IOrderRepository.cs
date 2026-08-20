using CloudOrders.Domain;

namespace CloudOrders.Application.Abstractions;

public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken cancellationToken);
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task ReloadAsync(Order order, CancellationToken cancellationToken);
}
