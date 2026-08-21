using Azure.Messaging.ServiceBus;
using System.Text.Json;

var conn = Environment.GetEnvironmentVariable("SERVICEBUS_CONNECTION_STRING");
if (string.IsNullOrWhiteSpace(conn))
{
    Console.Error.WriteLine("Set SERVICEBUS_CONNECTION_STRING env var first.");
    return 1;
}

var queue = "order-placed";
await using var client = new ServiceBusClient(conn);
var receiver = client.CreateReceiver(queue, new ServiceBusReceiverOptions { SubQueue = SubQueue.DeadLetter });
var peeked = await receiver.PeekMessagesAsync(maxMessages: 10);
if (!peeked.Any())
{
    Console.WriteLine("No dead-letter messages found.");
    return 0;
}

foreach (var m in peeked)
{
    Console.WriteLine("---- Message ----");
    Console.WriteLine($"MessageId: {m.MessageId}");
    Console.WriteLine($"CorrelationId: {m.CorrelationId}");
    Console.WriteLine($"Subject: {m.Subject}");
    Console.WriteLine($"Enqueued: {m.EnqueuedTime}");
    Console.WriteLine($"DeliveryCount: {m.DeliveryCount}");

    if (m.ApplicationProperties.TryGetValue("DeadLetterReason", out var reason))
        Console.WriteLine($"DeadLetterReason (app prop): {reason}");
    if (m.ApplicationProperties.TryGetValue("DeadLetterErrorDescription", out var desc))
        Console.WriteLine($"DeadLetterErrorDescription (app prop): {desc}");

    // ServiceBusReceivedMessage exposes dead-letter props via DeadLetterReason/Description only on receive. They may be in SystemProperties
    // We'll show all properties and body
    Console.WriteLine("Properties:");
    foreach (var kv in m.ApplicationProperties)
        Console.WriteLine($"  {kv.Key}: {kv.Value}");

    try
    {
        var body = m.Body.ToString();
        Console.WriteLine("Body:");
        try
        {
            var doc = JsonDocument.Parse(body);
            Console.WriteLine(JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            Console.WriteLine(body);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to read body: {ex.Message}");
    }
}

return 0;
