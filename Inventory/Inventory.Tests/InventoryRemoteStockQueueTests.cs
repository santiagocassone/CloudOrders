using Azure.Messaging.ServiceBus;
using Inventory.Application;
using System.Net.Http.Json;
using System.Text.Json;

namespace Inventory.Tests;

public sealed class InventoryRemoteStockQueueTests
{
    [Fact]
    public async Task PostReserve_ToRemoteAzureInventory_PublishesStockReservedToStockResultsQueue()
    {
        var baseUrl = Environment.GetEnvironmentVariable("INVENTORY_BASE_URL");
        var connectionString = Environment.GetEnvironmentVariable("SERVICEBUS_CONNECTION_STRING")
            ?? Environment.GetEnvironmentVariable("ServiceBus__ConnectionString");

        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        var orderId = Guid.NewGuid();
        var productId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        using var httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl, UriKind.Absolute)
        };

        var response = await httpClient.PostAsJsonAsync(
            "/api/inventory/reserve",
            new { orderId, productId, quantity = 1 });

        response.EnsureSuccessStatusCode();

        await using var serviceBusClient = new ServiceBusClient(connectionString);
        var receiver = serviceBusClient.CreateReceiver("stock-results");

        var deadline = DateTimeOffset.UtcNow.AddMinutes(2);
        ServiceBusReceivedMessage? found = null;

        while (DateTimeOffset.UtcNow < deadline && found is null)
        {
            var batch = await receiver.ReceiveMessagesAsync(10, TimeSpan.FromSeconds(5));
            if (batch.Count == 0)
            {
                continue;
            }

            foreach (var message in batch)
            {
                var matches = string.Equals(message.CorrelationId, orderId.ToString(), StringComparison.Ordinal) &&
                    string.Equals(message.Subject, nameof(StockReserved), StringComparison.Ordinal) &&
                    message.ApplicationProperties.TryGetValue("eventType", out var eventType) &&
                    string.Equals(eventType?.ToString(), nameof(StockReserved), StringComparison.Ordinal);

                if (matches)
                {
                    found = message;
                    break;
                }

                await receiver.AbandonMessageAsync(message);
            }
        }

        Assert.NotNull(found);
        var payload = JsonSerializer.Deserialize<StockReserved>(found!.Body.ToString());
        Assert.NotNull(payload);
        Assert.Equal(orderId, payload.OrderId);
        Assert.Equal(orderId.ToString(), found.CorrelationId);

        await receiver.CompleteMessageAsync(found);
    }
}
