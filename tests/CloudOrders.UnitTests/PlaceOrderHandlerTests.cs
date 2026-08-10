using CloudOrders.Application.Abstractions;
using CloudOrders.Application.Orders;
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
        var placeOrderHandler = new PlaceOrderHandler(repoMock.Object);
        var placeOrderCommand = new PlaceOrderCommand(Guid.NewGuid(), 100m);

        //Act
        var result = await placeOrderHandler.HandleAsync(placeOrderCommand, CancellationToken.None);

        //Assert
        Assert.NotEqual(Guid.Empty, result);
        repoMock.Verify(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_TotalIsZeroOrLess_ThrowsArgumentException()
    {
        //Arrange
        var repoMock = new Mock<IOrderRepository>();
        var placeOrderHandler = new PlaceOrderHandler(repoMock.Object);
        var placeOrderCommand = new PlaceOrderCommand(Guid.NewGuid(), 0m);

        //Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => placeOrderHandler.HandleAsync(placeOrderCommand, CancellationToken.None));
        repoMock.Verify(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}