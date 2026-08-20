using Azure.Messaging.ServiceBus;
using CloudOrders.Application.Orders;
using CloudOrders.Contracts;
using System.Text.Json;

namespace CloudOrders.Infrastructure.Messaging.ServiceBus;

public sealed class StockResultMessageMapper
{
    public StockResult Map(ServiceBusReceivedMessage message)
    {
        return message.Subject switch
        {
            nameof(StockReserved) => MapStockReserved(message),
            nameof(StockRejected) => MapStockRejected(message),
            _ => throw new InvalidIntegrationMessageException($"Unsupported message subject '{message.Subject}'.")
        };
    }

    private static StockResult MapStockReserved(ServiceBusReceivedMessage message)
    {
        try
        {
            var reserved = message.Body.ToObjectFromJson<StockReserved>()
                ?? throw new InvalidIntegrationMessageException("StockReserved message body is null.");

            return new StockResult(reserved.OrderId, StockResultStatus.Confirmed, null);
        }
        catch (JsonException ex)
        {
            throw new InvalidIntegrationMessageException("Invalid StockReserved message body.", ex);
        }
    }

    private static StockResult MapStockRejected(ServiceBusReceivedMessage message)
    {
        try
        {
            var rejected = message.Body.ToObjectFromJson<StockRejected>()
                ?? throw new InvalidIntegrationMessageException("StockRejected message body is null.");

            return new StockResult(rejected.OrderId, StockResultStatus.Rejected, rejected.Reason);
        }
        catch (JsonException ex)
        {
            throw new InvalidIntegrationMessageException("Invalid StockRejected message body.", ex);
        }
    }
}