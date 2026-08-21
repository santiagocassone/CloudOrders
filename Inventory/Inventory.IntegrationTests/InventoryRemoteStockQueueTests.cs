using Azure.Messaging.ServiceBus;
using Inventory.Application;
using System.Net.Http.Json;
using System.Text.Json;

namespace Inventory.IntegrationTests;

public sealed class InventoryRemoteStockQueueTests
{
    [Fact]
    [Trait("Category", "Prod")]
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
        long? sequenceNumber = null;

        while (DateTimeOffset.UtcNow < deadline && found is null)
        {
            IReadOnlyList<ServiceBusReceivedMessage> batch = sequenceNumber is null
                ? await receiver.PeekMessagesAsync(25, cancellationToken: CancellationToken.None)
                : await receiver.PeekMessagesAsync(25, sequenceNumber.Value, CancellationToken.None);

            if (batch.Count == 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(2));
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
            }

            if (found is null)
            {
                sequenceNumber = batch[^1].SequenceNumber + 1;
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }

        Assert.NotNull(found);
        var payload = JsonSerializer.Deserialize<StockReserved>(found!.Body.ToString());
        Assert.NotNull(payload);
        Assert.Equal(orderId, payload.OrderId);
        Assert.Equal(orderId.ToString(), found.CorrelationId);
    }
}
