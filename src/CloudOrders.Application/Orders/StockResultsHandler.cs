using CloudOrders.Application.Abstractions;
using CloudOrders.Domain;

namespace CloudOrders.Application.Orders
{
    public sealed class StockResultsHandler
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IProcessedMessageRepository _processedMessageRepository;

        public StockResultsHandler(IOrderRepository orderRepository, IUnitOfWork unitOfWork, IProcessedMessageRepository processedMessageRepository)
        {
            _orderRepository = orderRepository;
            _unitOfWork = unitOfWork;
            _processedMessageRepository = processedMessageRepository;
        }

        public async Task HandleStockResultAsync(string messageId, StockResult stockResult, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(messageId))
            {
                throw new ArgumentException("Message ID is required.", nameof(messageId));
            }

            if (await _processedMessageRepository.ExistsAsync(messageId, cancellationToken))
            {
                return;
            }

            var order = await _orderRepository.GetByIdAsync(stockResult.OrderId, cancellationToken);

            if (order is null)
            {
                throw new InvalidOperationException($"Order with ID {stockResult.OrderId} not found.");
            }

            switch (stockResult.Status)
            {
                case StockResultStatus.Confirmed:
                    order.Confirm();
                    break;

                case StockResultStatus.Rejected:
                    order.Reject();
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(stockResult.Status),
                        stockResult.Status,
                        "Unsupported stock result status.");
            }

            await _processedMessageRepository.AddAsync(messageId, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
