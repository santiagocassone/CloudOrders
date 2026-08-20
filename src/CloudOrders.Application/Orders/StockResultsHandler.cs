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
                throw new OrderNotFoundException(stockResult.OrderId);
            }

            await _processedMessageRepository.AddAsync(messageId, cancellationToken);

            ApplyStockResult(order, stockResult);

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (ConcurrencyConflictException)
            {
                await _orderRepository.ReloadAsync(order, cancellationToken);

                ApplyStockResult(order, stockResult);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }

        private static void ApplyStockResult(Order order, StockResult stockResult)
        {
            switch (stockResult.Status)
            {
                case StockResultStatus.Confirmed:
                    if (order.Status == OrderStatus.Confirmed)
                    {
                        return;
                    }

                    order.Confirm();
                    break;

                case StockResultStatus.Rejected:
                    if (order.Status == OrderStatus.Rejected)
                    {
                        return;
                    }

                    order.Reject();
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(stockResult.Status), stockResult.Status, "Unsupported stock result status.");
            }            
        }
    }
}
