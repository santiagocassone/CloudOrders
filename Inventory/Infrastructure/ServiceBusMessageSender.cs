using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;

namespace Inventory.Infrastructure;

public sealed class ServiceBusMessageSender : IServiceBusMessageSender, IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusSender _sender;

    public ServiceBusMessageSender(IOptions<ServiceBusOptions> options)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.ConnectionString))
        {
            throw new InvalidOperationException("ServiceBus:ConnectionString must be configured when UseInMemory is false.");
        }

        _client = new ServiceBusClient(settings.ConnectionString);
        _sender = _client.CreateSender(settings.StockResultsQueueName);
    }

    public Task SendAsync(ServiceBusMessage message, CancellationToken cancellationToken) =>
        _sender.SendMessageAsync(message, cancellationToken);

    public async ValueTask DisposeAsync()
    {
        await _sender.DisposeAsync();
        await _client.DisposeAsync();
    }
}
