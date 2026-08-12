using Azure.Messaging.ServiceBus;
using CloudOrders.Application.Abstractions;
using CloudOrders.Contracts;
using System.Text.Json;

namespace CloudOrders.Infrastructure.Messaging.ServiceBus;

public sealed class AzureServiceBusOrderEventPublisher : IOrderEventPublisher
{
    private readonly ServiceBusSender _serviceBusSender;
    public AzureServiceBusOrderEventPublisher(ServiceBusSender serviceBusSender)
    {
        _serviceBusSender = serviceBusSender;
    }
    public async Task PublishOrderPlacedAsync(OrderPlaced message, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(message);

        var serviceBusMessage = new ServiceBusMessage(json)
        {
            ContentType = "application/json"
        };

        await _serviceBusSender.SendMessageAsync(serviceBusMessage, cancellationToken);
    }
}