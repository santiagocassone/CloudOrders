using Inventory.Application;
using Inventory.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Inventory.Tests;

public sealed class OrderSubmissionServiceTests
{
    [Fact]
    public async Task ReserveAsync_ReservesAvailableStockAndPublishesStockReserved()
    {
        var completion = new TaskCompletionSource();
        var publisher = new Mock<IOrderEventsPublisher>();
        publisher.Setup(x => x.PublishAsync(It.IsAny<StockReserved>(), It.IsAny<CancellationToken>()))
            .Returns(completion.Task);

        await using var dbContext = CreateDbContext();
        var service = new OrderSubmissionService(dbContext, publisher.Object);

        var reservation = service.ReserveAsync(Guid.NewGuid(), Guid.Parse("11111111-1111-1111-1111-111111111111"), 2, CancellationToken.None);

        Assert.False(reservation.IsCompleted);
        completion.SetResult();
        var result = await reservation;

        Assert.True(result.Reserved);
        Assert.NotNull(result.ReservedEvent);
        publisher.Verify(x => x.PublishAsync(
            It.Is<StockReserved>(message => message.OrderId == result.ReservedEvent!.OrderId),
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task ReserveAsync_WhenStockIsInsufficient_PublishesStockRejected()
    {
        var publisher = new Mock<IOrderEventsPublisher>();
        publisher.Setup(x => x.PublishAsync(It.IsAny<StockRejected>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await using var dbContext = CreateDbContext();
        var service = new OrderSubmissionService(dbContext, publisher.Object);

        var orderId = Guid.NewGuid();
        var result = await service.ReserveAsync(orderId, Guid.Parse("11111111-1111-1111-1111-111111111111"), 999, CancellationToken.None);

        Assert.False(result.Reserved);
        Assert.NotNull(result.RejectedEvent);
        publisher.Verify(x => x.PublishAsync(
            It.Is<StockRejected>(message =>
                message.OrderId == orderId &&
                message.Reason.Contains("Insufficient stock")),
            CancellationToken.None), Times.Once);
    }

    private static InventoryDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase($"inventory-tests-{Guid.NewGuid()}")
            .Options;

        var dbContext = new InventoryDbContext(options);
        dbContext.InventoryItems.AddRange(
            new InventoryItem
            {
                ProductId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                AvailableQuantity = 10,
                ReservedQuantity = 0,
                UpdatedAt = DateTime.UtcNow
            },
            new InventoryItem
            {
                ProductId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                AvailableQuantity = 25,
                ReservedQuantity = 0,
                UpdatedAt = DateTime.UtcNow
            });
        dbContext.SaveChanges();

        return dbContext;
    }
}
