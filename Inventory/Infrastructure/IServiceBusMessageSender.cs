using Azure.Messaging.ServiceBus;

namespace Inventory.Infrastructure;

public interface IServiceBusMessageSender
{
    Task SendAsync(ServiceBusMessage message, CancellationToken cancellationToken);
}
