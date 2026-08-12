namespace CloudOrders.Infrastructure.Persistence;

public sealed class ProcessedMessage
{
    public string MessageId { get; private set; } = string.Empty;

    public DateTime ProcessedAtUtc { get; private set; }

    private ProcessedMessage()
    {
    }

    public static ProcessedMessage Create(string messageId)
    {
        if (string.IsNullOrWhiteSpace(messageId))
        {
            throw new ArgumentException(
                "Message ID cannot be empty.",
                nameof(messageId));
        }

        return new ProcessedMessage
        {
            MessageId = messageId,
            ProcessedAtUtc = DateTime.UtcNow
        };
    }
}