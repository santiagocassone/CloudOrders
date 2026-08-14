using Azure.Messaging.ServiceBus;
using Moq;
using Inventory.Application;
using Inventory.Infrastructure;

namespace Inventory.Tests;

public sealed class ServiceBusOrderEventsPublisherTests
{
    [Fact]
    public async Task PublishAsync_MapsStockReservedEventToServiceBusMetadata()
    {
        var sender = new Mock<IServiceBusMessageSender>();
        sender.Setup(x => x.SendAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var publisher = new ServiceBusOrderEventsPublisher(sender.Object);
        var order = new StockReserved(Guid.NewGuid(), DateTime.UtcNow);

        await publisher.PublishAsync(order, CancellationToken.None);

        sender.Verify(x => x.SendAsync(
            It.Is<ServiceBusMessage>(message =>
                !string.IsNullOrWhiteSpace(message.MessageId) &&
                message.MessageId != order.OrderId.ToString() &&
                message.CorrelationId == order.OrderId.ToString() &&
                message.Subject == nameof(StockReserved) &&
                message.ApplicationProperties["eventType"].Equals(nameof(StockReserved))),
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_MapsStockRejectedEventToServiceBusMetadata()
    {
        var sender = new Mock<IServiceBusMessageSender>();
        sender.Setup(x => x.SendAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var publisher = new ServiceBusOrderEventsPublisher(sender.Object);
        var order = new StockRejected(Guid.NewGuid(), "Insufficient stock", DateTime.UtcNow);

        await publisher.PublishAsync(order, CancellationToken.None);

        sender.Verify(x => x.SendAsync(
            It.Is<ServiceBusMessage>(message =>
                !string.IsNullOrWhiteSpace(message.MessageId) &&
                message.MessageId != order.OrderId.ToString() &&
                message.CorrelationId == order.OrderId.ToString() &&
                message.Subject == nameof(StockRejected) &&
                message.ApplicationProperties["eventType"].Equals(nameof(StockRejected))),
            CancellationToken.None), Times.Once);
    }
}
