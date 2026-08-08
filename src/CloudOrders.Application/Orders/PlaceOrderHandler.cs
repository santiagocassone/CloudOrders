using CloudOrders.Application.Abstractions;
using CloudOrders.Domain;

namespace CloudOrders.Application.Orders;

public sealed class PlaceOrderHandler
{
    private readonly IOrderRepository _orderRepository;

    public PlaceOrderHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<Guid> HandleAsync(PlaceOrderCommand command, CancellationToken cancellationToken)
    {
        var order = Order.Create(command.CustomerId, command.Total);

        await _orderRepository.AddAsync(order, cancellationToken);

        return order.Id;
    }
}