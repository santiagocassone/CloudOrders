using Azure.Messaging.ServiceBus;
using Inventory.Application;

namespace Inventory.Infrastructure;

public sealed class ServiceBusOrderEventsPublisher(IServiceBusMessageSender sender) : IOrderEventsPublisher
{
    public Task PublishAsync(StockReserved reserved, CancellationToken cancellationToken)
    {
        var message = new ServiceBusMessage(BinaryData.FromObjectAsJson(reserved))
        {
            MessageId = reserved.OrderId.ToString(),
            Subject = nameof(StockReserved),
            ContentType = "application/json"
        };
        message.ApplicationProperties["eventType"] = nameof(StockReserved);

        return sender.SendAsync(message, cancellationToken);
    }

    public Task PublishAsync(StockRejected rejected, CancellationToken cancellationToken)
    {
        var message = new ServiceBusMessage(BinaryData.FromObjectAsJson(rejected))
        {
            MessageId = rejected.OrderId.ToString(),
            Subject = nameof(StockRejected),
            ContentType = "application/json"
        };
        message.ApplicationProperties["eventType"] = nameof(StockRejected);

        return sender.SendAsync(message, cancellationToken);
    }
}
