using CloudOrders.Application.Abstractions;
using CloudOrders.Application.Orders;
using CloudOrders.Domain;
using Moq;

namespace CloudOrders.UnitTests
{
    public class StockResultsHandlerTests
    {
        [Fact]
        public async Task HandleStockResultsAsync_WhenMessageIdIsNullOrEmpty_ThrowArgumentException()
        {
            //Arrange
            var repoMock = new Mock<IOrderRepository>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();
            var processedMessageRepoMock = new Mock<IProcessedMessageRepository>();
            var stockResultsHandler = new StockResultsHandler(repoMock.Object, unitOfWorkMock.Object, processedMessageRepoMock.Object);

            var stockResult = new StockResult(Guid.NewGuid(), StockResultStatus.Confirmed, null);
            var messageId = String.Empty;

            //Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => stockResultsHandler.HandleStockResultAsync(messageId, stockResult, CancellationToken.None));

            processedMessageRepoMock.Verify(r => r.ExistsAsync(messageId, It.IsAny<CancellationToken>()), Times.Never);
            repoMock.Verify(r => r.GetByIdAsync(stockResult.OrderId, It.IsAny<CancellationToken>()), Times.Never);
            processedMessageRepoMock.Verify(r => r.AddAsync(messageId, It.IsAny<CancellationToken>()), Times.Never);
            unitOfWorkMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task HandleStockResultsAsync_WhenMessageIdExists_Returns()
        {
            //Arrange
            var repoMock = new Mock<IOrderRepository>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();
            var processedMessageRepoMock = new Mock<IProcessedMessageRepository>();
            var stockResultsHandler = new StockResultsHandler(repoMock.Object, unitOfWorkMock.Object, processedMessageRepoMock.Object);

            var stockResult = new StockResult(Guid.NewGuid(), StockResultStatus.Confirmed, null);
            var messageId = Guid.NewGuid().ToString();

            processedMessageRepoMock.Setup(r => r.ExistsAsync(messageId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            //Act & Assert
            var exception = await Record.ExceptionAsync(() => stockResultsHandler.HandleStockResultAsync(messageId, stockResult, CancellationToken.None));

            Assert.Null(exception);

            processedMessageRepoMock.Verify(r => r.ExistsAsync(messageId, It.IsAny<CancellationToken>()), Times.Once);
            repoMock.Verify(r => r.GetByIdAsync(stockResult.OrderId, It.IsAny<CancellationToken>()), Times.Never);
            processedMessageRepoMock.Verify(r => r.AddAsync(messageId, It.IsAny<CancellationToken>()), Times.Never);
            unitOfWorkMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task HandleStockResultsAsync_WhenOrderIdNotFound_ThrowInvalidOperationException()
        {
            //Arrange
            var repoMock = new Mock<IOrderRepository>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();
            var processedMessageRepoMock = new Mock<IProcessedMessageRepository>();
            var stockResultsHandler = new StockResultsHandler(repoMock.Object, unitOfWorkMock.Object, processedMessageRepoMock.Object);

            var stockResult = new StockResult(Guid.NewGuid(), StockResultStatus.Confirmed, null);
            var messageId = Guid.NewGuid().ToString();

            processedMessageRepoMock.Setup(r => r.ExistsAsync(messageId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
            repoMock.Setup(r => r.GetByIdAsync(stockResult.OrderId, It.IsAny<CancellationToken>())).ReturnsAsync((Order?)null);

            //Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => stockResultsHandler.HandleStockResultAsync(messageId, stockResult, CancellationToken.None));

            processedMessageRepoMock.Verify(r => r.ExistsAsync(messageId, It.IsAny<CancellationToken>()), Times.Once);
            repoMock.Verify(r => r.GetByIdAsync(stockResult.OrderId, It.IsAny<CancellationToken>()), Times.Once);
            processedMessageRepoMock.Verify(r => r.AddAsync(messageId, It.IsAny<CancellationToken>()), Times.Never);
            unitOfWorkMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task HandleStockResultsAsync_WhenStatusIsInvalid_ThrowArgumentOutOfRangeException()
        {
            // Arrange
            var repoMock = new Mock<IOrderRepository>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();
            var processedMessageRepoMock = new Mock<IProcessedMessageRepository>();
            var stockResultsHandler = new StockResultsHandler(repoMock.Object, unitOfWorkMock.Object, processedMessageRepoMock.Object);

            var order = Order.Create(Guid.NewGuid(), new List<OrderItem> { OrderItem.Create(Guid.NewGuid(), 1, 10m) });
            var messageId = Guid.NewGuid().ToString();
            var stockResult = new StockResult(order.Id, (StockResultStatus)999, null);

            processedMessageRepoMock.Setup(r => r.ExistsAsync(messageId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
            repoMock.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => stockResultsHandler.HandleStockResultAsync(messageId, stockResult, CancellationToken.None));

            processedMessageRepoMock.Verify(r => r.ExistsAsync(messageId, It.IsAny<CancellationToken>()), Times.Once);
            repoMock.Verify(r => r.GetByIdAsync(order.Id, CancellationToken.None), Times.Once);
            processedMessageRepoMock.Verify(r => r.AddAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),Times.Never);
            unitOfWorkMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task HandleStockResultsAsync_WhenReserved_ConfirmOrderAndUpdate()
        {
            //Arrange
            var repoMock = new Mock<IOrderRepository>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();
            var processedMessageRepoMock = new Mock<IProcessedMessageRepository>();
            var stockResultsHandler = new StockResultsHandler(repoMock.Object, unitOfWorkMock.Object, processedMessageRepoMock.Object);

            var customerId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var messageId = Guid.NewGuid().ToString();
            var order = Order.Create(customerId, new List<OrderItem> { OrderItem.Create(productId, 1, 10m) });
            var stockResult = new StockResult(order.Id, StockResultStatus.Confirmed, null);

            processedMessageRepoMock.Setup(r => r.ExistsAsync(messageId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
            repoMock.Setup(r => r.GetByIdAsync(order.Id, CancellationToken.None)).ReturnsAsync(order);
            unitOfWorkMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            //Act
            await stockResultsHandler.HandleStockResultAsync(messageId, stockResult, CancellationToken.None);

            //Assert
            Assert.Equal(OrderStatus.Confirmed, order.Status);

            processedMessageRepoMock.Verify(r => r.ExistsAsync(messageId, It.IsAny<CancellationToken>()), Times.Once);
            repoMock.Verify(r => r.GetByIdAsync(order.Id, CancellationToken.None), Times.Once);
            processedMessageRepoMock.Verify(r => r.AddAsync(messageId, It.IsAny<CancellationToken>()), Times.Once);
            unitOfWorkMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task HandleStockResultsAsync_WhenRejected_RejectOrderAndUpdate()
        {
            //Arrange
            var repoMock = new Mock<IOrderRepository>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();
            var processedMessageRepoMock = new Mock<IProcessedMessageRepository>();
            var stockResultsHandler = new StockResultsHandler(repoMock.Object, unitOfWorkMock.Object, processedMessageRepoMock.Object);

            var customerId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var messageId = Guid.NewGuid().ToString();
            var order = Order.Create(customerId, new List<OrderItem> { OrderItem.Create(productId, 1, 10m) });
            var stockResult = new StockResult(order.Id, StockResultStatus.Rejected, "Out of stock");

            processedMessageRepoMock.Setup(r => r.ExistsAsync(messageId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
            repoMock.Setup(r => r.GetByIdAsync(order.Id, CancellationToken.None)).ReturnsAsync(order);
            unitOfWorkMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            //Act
            await stockResultsHandler.HandleStockResultAsync(messageId, stockResult, CancellationToken.None);

            //Assert
            Assert.Equal(OrderStatus.Rejected, order.Status);

            processedMessageRepoMock.Verify(r => r.ExistsAsync(messageId, It.IsAny<CancellationToken>()), Times.Once);
            repoMock.Verify(r => r.GetByIdAsync(order.Id, CancellationToken.None), Times.Once);
            processedMessageRepoMock.Verify(r => r.AddAsync(messageId, It.IsAny<CancellationToken>()), Times.Once);
            unitOfWorkMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task HandleStockResultsAsync_WhenSameResultTwice_ShouldNotChangeStatus()
        {
            //Arrange
            var repoMock = new Mock<IOrderRepository>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();
            var processedMessageRepoMock = new Mock<IProcessedMessageRepository>();
            var stockResultsHandler = new StockResultsHandler(repoMock.Object, unitOfWorkMock.Object, processedMessageRepoMock.Object);

            var customerId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var messageId = Guid.NewGuid().ToString();
            var order = Order.Create(customerId, new List<OrderItem> { OrderItem.Create(productId, 1, 10m) });
            var stockResultConfirmed = new StockResult(order.Id, StockResultStatus.Confirmed, null);

            repoMock.Setup(r => r.GetByIdAsync(order.Id, CancellationToken.None)).ReturnsAsync(order);
            processedMessageRepoMock.SetupSequence(r => r.ExistsAsync(messageId, It.IsAny<CancellationToken>())).ReturnsAsync(false).ReturnsAsync(true);
            unitOfWorkMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            //Act
            await stockResultsHandler.HandleStockResultAsync(messageId, stockResultConfirmed, CancellationToken.None);
            await stockResultsHandler.HandleStockResultAsync(messageId, stockResultConfirmed, CancellationToken.None);

            //Assert
            Assert.Equal(OrderStatus.Confirmed, order.Status);

            processedMessageRepoMock.Verify(r => r.ExistsAsync(messageId, It.IsAny<CancellationToken>()), Times.Exactly(2));
            repoMock.Verify(r => r.GetByIdAsync(order.Id, CancellationToken.None), Times.Once);
            processedMessageRepoMock.Verify(r => r.AddAsync(messageId, It.IsAny<CancellationToken>()), Times.Once);
            unitOfWorkMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
