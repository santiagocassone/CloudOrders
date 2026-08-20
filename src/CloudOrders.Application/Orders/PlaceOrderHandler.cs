using CloudOrders.Application.Abstractions;
using CloudOrders.Contracts;
using CloudOrders.Domain;

namespace CloudOrders.Application.Orders;

public sealed class PlaceOrderHandler
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderEventPublisher _orderEventPublisher;
    private readonly IUnitOfWork _unitOfWork;

    public PlaceOrderHandler(IOrderRepository orderRepository, IOrderEventPublisher orderEventPublisher, IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _orderEventPublisher = orderEventPublisher;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> HandleAsync(PlaceOrderCommand command, CancellationToken cancellationToken)
    {

        var orderItems = command.Items.Select(item => OrderItem.Create(item.ProductId, item.Quantity, item.UnitPrice));
        var order = Order.Create(command.CustomerId, orderItems);

        await _orderRepository.AddAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _orderEventPublisher.PublishOrderPlacedAsync(new OrderPlaced(order.Id, orderItems.Select(item => new OrderPlacedItem(item.ProductId, item.Quantity)).ToList()), cancellationToken);

        return order.Id;
    }
}