using Azure.Identity;
using Azure.Messaging.ServiceBus;
using CloudOrders.Contracts;
using CloudOrders.Domain;
using CloudOrders.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables()
    .Build();

var connectionString = configuration.GetConnectionString("CloudOrders")
    ?? throw new InvalidOperationException("ConnectionStrings:CloudOrders is required.");

var serviceBusNamespace = configuration["ServiceBus:FullyQualifiedNamespace"];

if (string.IsNullOrWhiteSpace(serviceBusNamespace))
{
    throw new InvalidOperationException("ServiceBus:FullyQualifiedNamespace is required.");
}

var stockResultsQueue = configuration["ServiceBus:StockResultsQueue"];

if (string.IsNullOrWhiteSpace(stockResultsQueue))
{
    throw new InvalidOperationException("ServiceBus:StockResultsQueue is required.");
}

var messageCount = configuration.GetValue<int>("LoadTest:MessageCount");

if (messageCount <= 0)
{
    throw new InvalidOperationException("LoadTest:MessageCount must be greater than zero.");
}

Console.WriteLine($"Load test configured for {messageCount} messages.");
Console.WriteLine($"Service Bus namespace: {serviceBusNamespace}");
Console.WriteLine($"Queue: {stockResultsQueue}");

var dbOptions = new DbContextOptionsBuilder<CloudOrdersDbContext>()
    .UseSqlServer(connectionString)
    .Options;

await using var dbContext = new CloudOrdersDbContext(dbOptions);

var orderIds = new List<Guid>();

for (var i = 0; i < messageCount; i++)
{
    var order = Order.Create(
        Guid.NewGuid(),
        new[]
        {
            OrderItem.Create(Guid.NewGuid(), 1, 10m)
        });

    dbContext.Orders.Add(order);
    orderIds.Add(order.Id);
}

await dbContext.SaveChangesAsync();

Console.WriteLine($"Created {orderIds.Count} pending orders.");

await using var serviceBusClient = new ServiceBusClient(
    serviceBusNamespace,
    new AzureCliCredential());

await using var sender = serviceBusClient.CreateSender(stockResultsQueue);

var publishedCount = 0;

while (publishedCount < orderIds.Count)
{
    using var batch = await sender.CreateMessageBatchAsync();

    while (publishedCount < orderIds.Count)
    {
        var stockReserved = new StockReserved(
            orderIds[publishedCount],
            DateTime.UtcNow);

        var message = new ServiceBusMessage(BinaryData.FromObjectAsJson(stockReserved))
        {
            MessageId = Guid.NewGuid().ToString(),
            Subject = nameof(StockReserved),
            ContentType = "application/json"
        };

        if (!batch.TryAddMessage(message))
        {
            if (batch.Count == 0)
            {
                throw new InvalidOperationException("A message is too large to fit in a Service Bus batch.");
            }

            break;
        }

        publishedCount++;
    }

    await sender.SendMessagesAsync(batch);

    Console.WriteLine($"Published {publishedCount}/{orderIds.Count} messages.");
}

Console.WriteLine("Load test messages published.");