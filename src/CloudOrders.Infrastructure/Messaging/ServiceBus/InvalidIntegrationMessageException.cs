namespace CloudOrders.Infrastructure.Messaging.ServiceBus;

public sealed class InvalidIntegrationMessageException : Exception
{
    public InvalidIntegrationMessageException(string message) : base(message)
    {
    }

    public InvalidIntegrationMessageException(string message, Exception innerException) : base(message, innerException)
    {
    }
}