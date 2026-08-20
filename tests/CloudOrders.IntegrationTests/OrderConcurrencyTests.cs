using CloudOrders.Application.Orders;
using CloudOrders.Domain;
using CloudOrders.Infrastructure;
using CloudOrders.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CloudOrders.IntegrationTests;

public class OrderConcurrencyTests
{
    [Fact]
    public async Task SaveChanges_WhenOrderWasModifiedByAnotherContext_ThrowsConcurrencyException()
    {
        // Arrange
        var databaseName = $"CloudOrdersIntegrationTests_{Guid.NewGuid():N}";

        var connectionString =
            $"Server=localhost;Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True;";

        var options = new DbContextOptionsBuilder<CloudOrdersDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        Guid orderId;

        await using (var setupContext = new CloudOrdersDbContext(options))
        {
            await setupContext.Database.MigrateAsync();

            var order = Order.Create(
                Guid.NewGuid(),
                new[]
                {
                    OrderItem.Create(Guid.NewGuid(), 1, 10m)
                });

            setupContext.Orders.Add(order);
            await setupContext.SaveChangesAsync();

            orderId = order.Id;
        }

        try
        {
            await using var contextA = new CloudOrdersDbContext(options);
            await using var contextB = new CloudOrdersDbContext(options);

            var orderA = await contextA.Orders.SingleAsync(o => o.Id == orderId);
            var orderB = await contextB.Orders.SingleAsync(o => o.Id == orderId);

            Assert.Equal(
                Convert.ToHexString(orderA.Version),
                Convert.ToHexString(orderB.Version));

            var originalVersion = Convert.ToHexString(orderA.Version);

            // Context B gana la carrera.
            orderB.Confirm();
            await contextB.SaveChangesAsync();

            Assert.NotEqual(
                originalVersion,
                Convert.ToHexString(orderB.Version));

            // Context A sigue teniendo la versión vieja.
            orderA.Confirm();

            // Act + Assert
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
                () => contextA.SaveChangesAsync());
        }
        finally
        {
            await using var cleanupContext =
                new CloudOrdersDbContext(options);

            await cleanupContext.Database.EnsureDeletedAsync();
        }
    }

    [Fact]
    public async Task HandleStockResult_WhenConcurrentUpdateAlreadyApplied_ReloadsAndProcessesMessage()
    {
        var databaseName = $"CloudOrdersIntegrationTests_{Guid.NewGuid():N}";

        var connectionString =
            $"Server=localhost;Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True;";

        var options = new DbContextOptionsBuilder<CloudOrdersDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        Guid orderId;

        await using (var setupContext = new CloudOrdersDbContext(options))
        {
            await setupContext.Database.MigrateAsync();

            var order = Order.Create(
                Guid.NewGuid(),
                new[]
                {
                OrderItem.Create(Guid.NewGuid(), 1, 10m)
                });

            setupContext.Orders.Add(order);
            await setupContext.SaveChangesAsync();

            orderId = order.Id;
        }

        try
        {
            await using var handlerContext = new CloudOrdersDbContext(options);
            await using var concurrentContext = new CloudOrdersDbContext(options);

            var staleOrder =
                await handlerContext.Orders.SingleAsync(o => o.Id == orderId);

            var concurrentOrder =
                await concurrentContext.Orders.SingleAsync(o => o.Id == orderId);

            var staleVersion = Convert.ToHexString(staleOrder.Version);

            concurrentOrder.Confirm();
            await concurrentContext.SaveChangesAsync();

            Assert.Equal(OrderStatus.Pending, staleOrder.Status);
            Assert.Equal(OrderStatus.Confirmed, concurrentOrder.Status);

            var orderRepository = new SqlOrderRepository(handlerContext);
            var processedMessageRepository =
                new SqlProcessedMessageRepository(handlerContext);
            var unitOfWork = new EfUnitOfWork(handlerContext);

            var handler = new StockResultsHandler(
                orderRepository,
                unitOfWork,
                processedMessageRepository);

            var messageId = Guid.NewGuid().ToString();

            var stockResult = new StockResult(
                orderId,
                StockResultStatus.Confirmed,
                null);

            await handler.HandleStockResultAsync(
                messageId,
                stockResult,
                CancellationToken.None);

            Assert.Equal(OrderStatus.Confirmed, staleOrder.Status);
            Assert.NotEqual(
                staleVersion,
                Convert.ToHexString(staleOrder.Version));

            await using var verificationContext =
                new CloudOrdersDbContext(options);

            var persistedOrder = await verificationContext.Orders
                .AsNoTracking()
                .SingleAsync(o => o.Id == orderId);

            var messageWasProcessed =
                await verificationContext.ProcessedMessages
                    .AsNoTracking()
                    .AnyAsync(x => x.MessageId == messageId);

            Assert.Equal(OrderStatus.Confirmed, persistedOrder.Status);
            Assert.True(messageWasProcessed);
        }
        finally
        {
            await using var cleanupContext =
                new CloudOrdersDbContext(options);

            await cleanupContext.Database.EnsureDeletedAsync();
        }
    }
}