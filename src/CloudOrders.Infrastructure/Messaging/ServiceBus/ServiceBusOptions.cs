public sealed class ServiceBusOptions
{
    public const string SectionName = "ServiceBus";

    public string FullyQualifiedNamespace { get; init; } = string.Empty;
    public string OrderPlacedQueue { get; init; } = string.Empty;
    public string StockResultsQueue { get; init; } = string.Empty;
    public int StockResultsMaxConcurrentCalls { get; init; } = 4;
}