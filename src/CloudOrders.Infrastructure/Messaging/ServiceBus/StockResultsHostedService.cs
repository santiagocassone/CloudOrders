using Azure.Messaging.ServiceBus;
using CloudOrders.Application.Orders;
using CloudOrders.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace CloudOrders.Infrastructure.Messaging.ServiceBus;

public sealed class StockResultsHostedService : IHostedService
{
    private readonly ServiceBusProcessor _processor;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly StockResultMessageMapper _mapper;
    private readonly ILogger<StockResultsHostedService> _logger;
    private readonly ServiceBusOptions _serviceBusOptions;
    private int _activeMessages;

    public StockResultsHostedService(ServiceBusProcessor processor, IServiceScopeFactory scopeFactory, StockResultMessageMapper mapper, ILogger<StockResultsHostedService> logger, IOptions<ServiceBusOptions> serviceBusOptions)
    {
        _processor = processor;
        _scopeFactory = scopeFactory;
        _mapper = mapper;
        _logger = logger;
        _serviceBusOptions = serviceBusOptions.Value;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {

        _processor.ProcessMessageAsync += ProcessMessageAsync;
        _processor.ProcessErrorAsync += ProcessErrorAsync;

        _logger.LogInformation("Starting stock results Service Bus processor. MaxConcurrentCalls: {MaxConcurrentCalls}", _serviceBusOptions.StockResultsMaxConcurrentCalls);

        await _processor.StartProcessingAsync(cancellationToken);

        _logger.LogInformation("Service Bus processor for stock results started.");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Service Bus processor for stock results.");

        await _processor.StopProcessingAsync(cancellationToken);

        _logger.LogInformation("Service Bus processor for stock results stopped.");
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        var activeMessages = Interlocked.Increment(ref _activeMessages);

        _logger.LogInformation("Stock result processing started. MessageId: {MessageId}, ActiveMessages: {ActiveMessages}", args.Message.MessageId, activeMessages);

        var stopwatch = Stopwatch.StartNew();
        var outcome = "Unknown";

        try
        {
            var message = args.Message;

            try
            {
                if (string.IsNullOrWhiteSpace(message.MessageId))
                {
                    throw new InvalidIntegrationMessageException("Service Bus message does not contain a valid MessageId.");
                }

                await using var scope = _scopeFactory.CreateAsyncScope();

                var handler = scope.ServiceProvider.GetRequiredService<StockResultsHandler>();
                var stockResult = _mapper.Map(message);

                await handler.HandleStockResultAsync(message.MessageId, stockResult, args.CancellationToken);
                outcome = "BusinessProcessed";
            }
            catch (InvalidIntegrationMessageException ex)
            {
                outcome = "InvalidIntegrationMessage";
                _logger.LogWarning(ex, "Invalid integration message. MessageId: {MessageId}, Subject: {Subject}, DeliveryCount: {DeliveryCount}", message.MessageId, message.Subject, message.DeliveryCount);

                await args.DeadLetterMessageAsync(message, "InvalidIntegrationMessage", ex.Message, args.CancellationToken);
                return;
            }
            catch (OrderNotFoundException ex)
            {
                outcome = "OrderNotFound";
                _logger.LogWarning(ex, "Order not found. MessageId: {MessageId}, Subject: {Subject}, DeliveryCount: {DeliveryCount}", message.MessageId, message.Subject, message.DeliveryCount);

                await args.DeadLetterMessageAsync(message, "OrderNotFound", ex.Message, args.CancellationToken);
                return;
            }
            catch (InvalidOrderStateTransitionException ex)
            {
                outcome = "InvalidOrderStateTransition";
                _logger.LogWarning(ex, "Invalid order state transition. MessageId: {MessageId}, Subject: {Subject}, DeliveryCount: {DeliveryCount}", message.MessageId, message.Subject, message.DeliveryCount);

                await args.DeadLetterMessageAsync(message, "InvalidOrderStateTransition", ex.Message, args.CancellationToken);
                return;
            }
            catch (Exception ex)
            {
                outcome = "Error";
                _logger.LogError(ex, "Error processing Service Bus message. MessageId: {MessageId}, Subject: {Subject}, DeliveryCount: {DeliveryCount}", message.MessageId, message.Subject, message.DeliveryCount);

                await args.AbandonMessageAsync(message, cancellationToken: args.CancellationToken);
                return;
            }

            try
            {
                await args.CompleteMessageAsync(message, args.CancellationToken);
                outcome = "Completed";
            }
            catch (Exception ex)
            {
                outcome = "CompletionFailed";
                _logger.LogError(ex, "Business processing succeeded, but Service Bus completion failed. MessageId: {MessageId}, Subject: {Subject}", message.MessageId, message.Subject);
            }
        }
        finally
        {
            stopwatch.Stop();
            _logger.LogInformation("Processed stock result. MessageId: {MessageId}. Outcome: {Outcome}. ElapsedTime: {ElapsedTime}ms", args.Message.MessageId, outcome, stopwatch.ElapsedMilliseconds);
            Interlocked.Decrement(ref _activeMessages);
        }
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "Service Bus processor error. Namespace: {Namespace}, Entity: {EntityPath}, Source: {ErrorSource}", args.FullyQualifiedNamespace, args.EntityPath, args.ErrorSource);

        return Task.CompletedTask;
    }
}