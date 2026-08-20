namespace CloudOrders.Domain
{
    public sealed class InvalidOrderStateTransitionException : Exception
    { 
        public InvalidOrderStateTransitionException(string message) : base(message) { }
        public InvalidOrderStateTransitionException(string message, Exception ex) : base(message, ex) { }
    }
}
