namespace Inventory.Infrastructure;

public sealed class ServiceBusOptions
{
    public const string SectionName = "ServiceBus";

    public bool UseInMemory { get; init; } = true;
    public string? ConnectionString { get; init; }
    public string StockResultsQueueName { get; init; } = "stock-results";
    public string OrderPlacedQueueName { get; init; } = "order-placed";
}
