using CloudOrders.Domain;

namespace CloudOrders.Application.Abstractions;

public interface IQuerySource
{
    IQueryable<Order> OrdersQuery { get; }
}