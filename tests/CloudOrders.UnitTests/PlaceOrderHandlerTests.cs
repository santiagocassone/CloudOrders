using CloudOrders.Application.Abstractions;
using CloudOrders.Application.Orders;
using CloudOrders.Contracts;
using CloudOrders.Domain;
using Moq;

namespace CloudOrders.UnitTests;

public class PlaceOrderHandlerTests
{
    [Fact]
    public async Task HandleAsync_ValidCommand_ReturnsNewOrderId()
    {
        //Arrange
        var repoMock = new Mock<IOrderRepository>();
        var eventPublisherMock = new Mock<IOrderEventPublisher>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var placeOrderHandler = new PlaceOrderHandler(repoMock.Object, eventPublisherMock.Object, unitOfWorkMock.Object);
        var productId = Guid.NewGuid();
        var placeOrderCommand = new PlaceOrderCommand(Guid.NewGuid(), new List<PlaceOrderItem>() { new PlaceOrderItem(productId, 2, 50m) });

        //Act
        var result = await placeOrderHandler.HandleAsync(placeOrderCommand, CancellationToken.None);

        //Assert
        Assert.NotEqual(Guid.Empty, result);
        repoMock.Verify(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Once);
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        eventPublisherMock.Verify(
            p => p.PublishOrderPlacedAsync(
                It.Is<OrderPlaced>(m =>
                    m.OrderId == result &&
                    m.Items.Count == 1 &&
                    m.Items.First().ProductId == productId &&
                    m.Items.First().Quantity == 2),
                It.IsAny<CancellationToken>()),
                Times.Once);
    }

    [Fact]
    public async Task HandleAsync_NoItem_ThrowsArgumentException()
    {
        //Arrange
        var repoMock = new Mock<IOrderRepository>();
        var eventPublisherMock = new Mock<IOrderEventPublisher>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var placeOrderHandler = new PlaceOrderHandler(repoMock.Object, eventPublisherMock.Object, unitOfWorkMock.Object);
        var placeOrderCommand = new PlaceOrderCommand(Guid.NewGuid(), Array.Empty<PlaceOrderItem>());

        //Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => placeOrderHandler.HandleAsync(placeOrderCommand, CancellationToken.None));
        repoMock.Verify(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        eventPublisherMock.Verify(
            p => p.PublishOrderPlacedAsync(It.IsAny<OrderPlaced>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_RepositoryThrowsException_DoesNotPublishEvent()
    {
        //Arrange
        var repoMock = new Mock<IOrderRepository>();
        var eventPublisherMock = new Mock<IOrderEventPublisher>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var placeOrderHandler = new PlaceOrderHandler(repoMock.Object, eventPublisherMock.Object, unitOfWorkMock.Object);
        var placeOrderCommand = new PlaceOrderCommand(Guid.NewGuid(), new List<PlaceOrderItem>
        {
            new PlaceOrderItem(Guid.NewGuid(), 2, 50m)
        });

        repoMock.Setup(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Repository error"));

        //Act & Assert
        await Assert.ThrowsAsync<Exception>(() => placeOrderHandler.HandleAsync(placeOrderCommand, CancellationToken.None));
        repoMock.Verify(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Once);
        unitOfWorkMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        eventPublisherMock.Verify(
            p => p.PublishOrderPlacedAsync(It.IsAny<OrderPlaced>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}