namespace Inventory.Infrastructure;

public sealed class ServiceBusOptions
{
    public const string SectionName = "ServiceBus";

    public bool UseInMemory { get; init; } = true;
    public string? ConnectionString { get; init; }
    public string TopicName { get; init; } = "order-fulfillment-events";
    public string SubscriptionName { get; init; } = "api-sub";
}
