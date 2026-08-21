using Azure.Messaging.ServiceBus;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace CloudOrders.IntegrationTests;

public sealed class OrderRemoteTests
{
    [Fact]
    [Trait("Category", "Prod")]
    public async Task PlaceOrder_OnRemoteCloudOrders_PublishesStockResultAndCanBeFetched()
    {
        var ordersBaseUrl = Environment.GetEnvironmentVariable("CLOUDORDERS_BASE_URL")
            ?? Environment.GetEnvironmentVariable("ORDERS_BASE_URL");
        var email = Environment.GetEnvironmentVariable("ORDERS_REMOTE_EMAIL");
        var password = Environment.GetEnvironmentVariable("ORDERS_REMOTE_PASSWORD");
        var serviceBusConnectionString = Environment.GetEnvironmentVariable("SERVICEBUS_CONNECTION_STRING")
            ?? Environment.GetEnvironmentVariable("ServiceBus__ConnectionString");
        var stockResultsQueue = Environment.GetEnvironmentVariable("STOCK_RESULTS_QUEUE_NAME") ?? "stock-results";

        if (string.IsNullOrWhiteSpace(ordersBaseUrl) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(serviceBusConnectionString))
        {
            return;
        }

        var productId = GetProductId();

        using var httpClient = new HttpClient
        {
            BaseAddress = new Uri(ordersBaseUrl, UriKind.Absolute)
        };

        var token = await LoginAsync(httpClient, email, password);
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var orderId = await PlaceOrderAsync(httpClient, productId);

        var getOrderResponse = await httpClient.GetAsync($"/api/orders/{orderId}");
        Assert.Equal(HttpStatusCode.OK, getOrderResponse.StatusCode);

        await using var serviceBusClient = new ServiceBusClient(serviceBusConnectionString);
        var receiver = serviceBusClient.CreateReceiver(stockResultsQueue);

        var found = await PeekStockResultByCorrelationIdAsync(receiver, orderId, TimeSpan.FromMinutes(2));
        Assert.NotNull(found);
        Assert.Equal(orderId.ToString(), found!.CorrelationId);
        Assert.True(found.ApplicationProperties.ContainsKey("eventType"));
    }

    private static Guid GetProductId()
    {
        var value = Environment.GetEnvironmentVariable("ORDERS_TEST_PRODUCT_ID");
        return Guid.TryParse(value, out var parsed)
            ? parsed
            : Guid.Parse("11111111-1111-1111-1111-111111111111");
    }

    private static async Task<string> LoginAsync(HttpClient httpClient, string email, string password)
    {
        var response = await httpClient.PostAsJsonAsync("/api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        if (!document.RootElement.TryGetProperty("token", out var tokenElement) ||
            string.IsNullOrWhiteSpace(tokenElement.GetString()))
        {
            throw new InvalidOperationException("Auth response does not contain token.");
        }

        return tokenElement.GetString()!;
    }

    private static async Task<Guid> PlaceOrderAsync(HttpClient httpClient, Guid productId)
    {
        var createResponse = await httpClient.PostAsJsonAsync(
            "/api/orders",
            new
            {
                customerId = Guid.NewGuid(),
                items = new[]
                {
                    new
                    {
                        productId,
                        quantity = 1,
                        unitPrice = 10m
                    }
                }
            });

        createResponse.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        if (!document.RootElement.TryGetProperty("id", out var idElement) ||
            !Guid.TryParse(idElement.GetString(), out var orderId))
        {
            throw new InvalidOperationException("Order creation response does not contain a valid id.");
        }

        return orderId;
    }

    private static async Task<ServiceBusReceivedMessage?> PeekStockResultByCorrelationIdAsync(
        ServiceBusReceiver receiver,
        Guid orderId,
        TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow.Add(timeout);
        long? sequenceNumber = null;

        while (DateTimeOffset.UtcNow < deadline)
        {
            IReadOnlyList<ServiceBusReceivedMessage> batch = sequenceNumber is null
                ? await receiver.PeekMessagesAsync(50, cancellationToken: CancellationToken.None)
                : await receiver.PeekMessagesAsync(50, sequenceNumber.Value, CancellationToken.None);

            if (batch.Count == 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(2));
                continue;
            }

            var found = batch.FirstOrDefault(message =>
                string.Equals(message.CorrelationId, orderId.ToString(), StringComparison.Ordinal));

            if (found is not null)
            {
                return found;
            }

            sequenceNumber = batch[^1].SequenceNumber + 1;
            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        return null;
    }
}
