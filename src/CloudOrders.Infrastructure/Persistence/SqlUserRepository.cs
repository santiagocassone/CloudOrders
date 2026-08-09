using CloudOrders.Application.Abstractions;
using CloudOrders.Domain;
using Microsoft.EntityFrameworkCore;

namespace CloudOrders.Infrastructure.Persistence;

public sealed class SqlUserRepository : IUserRepository
{
    private readonly CloudOrdersDbContext _dbContext;

    public SqlUserRepository(CloudOrdersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        return await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }
}