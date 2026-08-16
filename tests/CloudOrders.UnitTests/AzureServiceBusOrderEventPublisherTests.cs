using Azure.Messaging.ServiceBus;
using CloudOrders.Contracts;
using CloudOrders.Infrastructure.Messaging.ServiceBus;
using System.Text.Json;

namespace CloudOrders.UnitTests;

public class AzureServiceBusOrderEventPublisherTests
{
    [Fact]
    public async Task PublishOrderPlacedAsync_SetsOrderIdMetadataAndJsonContentType()
    {
        var sender = new CapturingServiceBusSender();
        var publisher = new AzureServiceBusOrderEventPublisher(sender);
        var orderPlaced = new OrderPlaced(Guid.NewGuid(), new List<OrderPlacedItem>
        {
            new(Guid.NewGuid(), 2)
        });

        await publisher.PublishOrderPlacedAsync(orderPlaced, CancellationToken.None);

        var sentMessage = Assert.Single(sender.SentMessages);
        Assert.Equal(orderPlaced.OrderId.ToString(), sentMessage.MessageId);
        Assert.Equal(orderPlaced.OrderId.ToString(), sentMessage.CorrelationId);
        Assert.Equal("application/json", sentMessage.ContentType);
    }

    [Fact]
    public async Task PublishOrderPlacedAsync_SerializesPayloadAsJson()
    {
        var sender = new CapturingServiceBusSender();
        var publisher = new AzureServiceBusOrderEventPublisher(sender);
        var orderPlaced = new OrderPlaced(Guid.NewGuid(), new List<OrderPlacedItem>
        {
            new(Guid.NewGuid(), 3)
        });

        await publisher.PublishOrderPlacedAsync(orderPlaced, CancellationToken.None);

        var sentMessage = Assert.Single(sender.SentMessages);
        var payload = JsonSerializer.Deserialize<OrderPlaced>(sentMessage.Body.ToString());

        Assert.NotNull(payload);
        Assert.Equal(orderPlaced.OrderId, payload.OrderId);
        Assert.Equal(orderPlaced.Items.Single().ProductId, payload.Items.Single().ProductId);
        Assert.Equal(orderPlaced.Items.Single().Quantity, payload.Items.Single().Quantity);
    }

    private sealed class CapturingServiceBusSender : ServiceBusSender
    {
        public List<ServiceBusMessage> SentMessages { get; } = new();

        public override Task SendMessageAsync(ServiceBusMessage message, CancellationToken cancellationToken = default)
        {
            SentMessages.Add(message);
            return Task.CompletedTask;
        }
    }
}
