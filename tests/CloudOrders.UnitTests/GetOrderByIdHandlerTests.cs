using System.Linq.Expressions;
using CloudOrders.Application.Abstractions;
using CloudOrders.Application.Orders;
using CloudOrders.Domain;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace CloudOrders.UnitTests;

public class GetOrderByIdHandlerTests
{
    [Fact]
    public async Task HandleAsync_ValidOrderId_ReturnsOrder()
    {
        //Arrange
        var repoMock = new Mock<IQuerySource>();
        var getOrderByIdHandler = new GetOrderByIdHandler(repoMock.Object);

        var order1 = Order.Create(Guid.NewGuid(), 100m);
        var orders = new List<Order> { order1 }.AsQueryable();
        var createOrderRequest = new GetOrderByIdQuery(order1.Id);
        repoMock.Setup(m => m.GetOrderByIdAsync(order1.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order1);

        var expected = new OrderDto(order1.Id, order1.CustomerId, order1.Total, order1.Status.ToString(), order1.CreatedAt);

        //Act
        var result = await getOrderByIdHandler.HandleAsync(createOrderRequest, CancellationToken.None);

        //Assert
        Assert.Equal(expected, result);
        repoMock.Verify(r => r.GetOrderByIdAsync(order1.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_InvalidOrderId_ReturnsNull()
    {
        //Arrange
        var repoMock = new Mock<IQuerySource>();
        var getOrderByIdHandler = new GetOrderByIdHandler(repoMock.Object);

        var createOrderRequest = new GetOrderByIdQuery(Guid.NewGuid());
        repoMock.Setup(m => m.GetOrderByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Order?)null);

        //Act
        var result = await getOrderByIdHandler.HandleAsync(createOrderRequest, CancellationToken.None);

        //Assert
        Assert.Null(result);
        repoMock.Verify(r => r.GetOrderByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}