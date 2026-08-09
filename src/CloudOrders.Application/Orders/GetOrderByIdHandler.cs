using CloudOrders.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CloudOrders.Application.Orders;

public sealed class GetOrderByIdHandler
{
    private readonly IQuerySource _querySource;

    public GetOrderByIdHandler(IQuerySource querySource)
    {
        _querySource = querySource;
    }

    public async Task<OrderDto?> HandleAsync(GetOrderByIdQuery query, CancellationToken cancellationToken)
    {
        var order = await _querySource.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == query.OrderId, cancellationToken);

        if (order is null)
        {
            return null;
        }

        return new OrderDto(order.Id, order.CustomerId, order.Total, order.Status.ToString(), order.CreatedAt);
    }

}