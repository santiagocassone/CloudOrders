using CloudOrders.Application.Abstractions;
using CloudOrders.Domain;
using Microsoft.EntityFrameworkCore;

namespace CloudOrders.Infrastructure.Persistence;

public sealed class SqlOrderRepository : IOrderRepository
{
    private readonly CloudOrdersDbContext _dbContext;

    public SqlOrderRepository(CloudOrdersDbContext cloudOrdersDbContext)
    {
        _dbContext = cloudOrdersDbContext;
    }

    public async Task AddAsync(Order order, CancellationToken cancellationToken)
    {
        await _dbContext.Orders.AddAsync(order, cancellationToken);
    }

    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.Orders.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task ReloadAsync(Order order, CancellationToken cancellationToken)
    {
        await _dbContext.Entry(order).ReloadAsync(cancellationToken);
    }
}