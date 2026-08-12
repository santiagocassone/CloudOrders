namespace Inventory.Infrastructure;

public sealed class ServiceBusOptions
{
    public const string SectionName = "ServiceBus";

    public bool UseInMemory { get; init; } = true;
    public string? ConnectionString { get; init; }
    public string FulfillmentTopicName { get; init; } = "order-fulfillment-events";
    public string OrderEventsTopicName { get; init; } = "order-events";
    public string InventorySubscriptionName { get; init; } = "inventory-sub";
}
