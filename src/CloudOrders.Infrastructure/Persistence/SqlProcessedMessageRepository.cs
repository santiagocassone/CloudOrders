using CloudOrders.Infrastructure;
using CloudOrders.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public sealed class SqlProcessedMessageRepository : IProcessedMessageRepository
{
    private readonly CloudOrdersDbContext _dbContext;

    public SqlProcessedMessageRepository(CloudOrdersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsAsync(string messageId, CancellationToken cancellationToken)
    {
        return _dbContext.ProcessedMessages.AnyAsync(x => x.MessageId == messageId, cancellationToken);
    }

    public Task AddAsync(string messageId, CancellationToken cancellationToken)
    {
        var processedMessage = ProcessedMessage.Create(messageId);

        _dbContext.ProcessedMessages.Add(processedMessage);

        return Task.CompletedTask;
    }
}