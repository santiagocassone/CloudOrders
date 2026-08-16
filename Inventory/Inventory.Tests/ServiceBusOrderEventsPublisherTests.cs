using Azure.Messaging.ServiceBus;
using Inventory.Application;
using Inventory.Infrastructure;
using Moq;
using System.Text.Json;

namespace Inventory.Tests;

public sealed class ServiceBusOrderEventsPublisherTests
{
    [Fact]
    public async Task PublishAsync_MapsStockReservedEventToServiceBusMetadata()
    {
        var sender = new Mock<IServiceBusMessageSender>();
        ServiceBusMessage? sentMessage = null;
        sender.Setup(x => x.SendAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Callback<ServiceBusMessage, CancellationToken>((message, _) => sentMessage = message)
            .Returns(Task.CompletedTask);
        var publisher = new ServiceBusOrderEventsPublisher(sender.Object);
        var order = new StockReserved(Guid.NewGuid(), DateTime.UtcNow);

        await publisher.PublishAsync(order, CancellationToken.None);

        sender.Verify(x => x.SendAsync(
            It.Is<ServiceBusMessage>(message =>
                !string.IsNullOrWhiteSpace(message.MessageId) &&
                message.MessageId != order.OrderId.ToString() &&
                message.CorrelationId == order.OrderId.ToString() &&
                message.ContentType == "application/json" &&
                message.Subject == nameof(StockReserved) &&
                message.ApplicationProperties["eventType"].Equals(nameof(StockReserved))),
            CancellationToken.None), Times.Once);

        var payload = JsonSerializer.Deserialize<StockReserved>(sentMessage!.Body.ToString());
        Assert.NotNull(payload);
        Assert.Equal(order.OrderId, payload.OrderId);
    }

    [Fact]
    public async Task PublishAsync_MapsStockRejectedEventToServiceBusMetadata()
    {
        var sender = new Mock<IServiceBusMessageSender>();
        ServiceBusMessage? sentMessage = null;
        sender.Setup(x => x.SendAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Callback<ServiceBusMessage, CancellationToken>((message, _) => sentMessage = message)
            .Returns(Task.CompletedTask);
        var publisher = new ServiceBusOrderEventsPublisher(sender.Object);
        var order = new StockRejected(Guid.NewGuid(), "Insufficient stock", DateTime.UtcNow);

        await publisher.PublishAsync(order, CancellationToken.None);

        sender.Verify(x => x.SendAsync(
            It.Is<ServiceBusMessage>(message =>
                !string.IsNullOrWhiteSpace(message.MessageId) &&
                message.MessageId != order.OrderId.ToString() &&
                message.CorrelationId == order.OrderId.ToString() &&
                message.ContentType == "application/json" &&
                message.Subject == nameof(StockRejected) &&
                message.ApplicationProperties["eventType"].Equals(nameof(StockRejected))),
            CancellationToken.None), Times.Once);

        var payload = JsonSerializer.Deserialize<StockRejected>(sentMessage!.Body.ToString());
        Assert.NotNull(payload);
        Assert.Equal(order.OrderId, payload.OrderId);
        Assert.Equal(order.Reason, payload.Reason);
    }
}
