using Azure.Messaging.ServiceBus;
using Inventory.Application;
using Microsoft.Extensions.Options;

namespace Inventory.Infrastructure;

public sealed class ServiceBusOrderPlacedConsumer : IHostedService, IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<ServiceBusOptions> _options;
    private readonly ILogger<ServiceBusOrderPlacedConsumer> _logger;
    private ServiceBusProcessor? _processor;

    public ServiceBusOrderPlacedConsumer(
        IServiceScopeFactory scopeFactory,
        IOptions<ServiceBusOptions> options,
        ILogger<ServiceBusOrderPlacedConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;

        var connectionString = _options.Value.ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ServiceBus:ConnectionString must be configured when UseInMemory is false.");
        }

        _client = new ServiceBusClient(connectionString);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var settings = _options.Value;

        _processor = _client.CreateProcessor(settings.OrderEventsTopicName, settings.InventorySubscriptionName, new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false,
            MaxConcurrentCalls = 1
        });

        _processor.ProcessMessageAsync += ProcessMessageAsync;
        _processor.ProcessErrorAsync += ProcessErrorAsync;

        await _processor.StartProcessingAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_processor is null)
        {
            return;
        }

        await _processor.StopProcessingAsync(cancellationToken);
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        var orderPlaced = args.Message.Body.ToObjectFromJson<OrderPlaced>();
        if (orderPlaced is null)
        {
            await args.DeadLetterMessageAsync(args.Message, "InvalidPayload", "Could not deserialize OrderPlaced message.", args.CancellationToken);
            return;
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<OrderSubmissionService>();

        foreach (var item in orderPlaced.Items)
        {
            await service.ReserveAsync(orderPlaced.OrderId, item.ProductId, item.Quantity, args.CancellationToken);
        }

        await args.CompleteMessageAsync(args.Message, args.CancellationToken);
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "Service Bus processing error on entity {EntityPath}", args.EntityPath);
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_processor is not null)
        {
            _processor.ProcessMessageAsync -= ProcessMessageAsync;
            _processor.ProcessErrorAsync -= ProcessErrorAsync;
            await _processor.DisposeAsync();
        }

        await _client.DisposeAsync();
    }
}
