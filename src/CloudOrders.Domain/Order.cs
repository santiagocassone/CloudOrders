namespace CloudOrders.Domain;

public class Order
{
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public decimal Total { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Order()
    {
        Status = OrderStatus.Pending;
    }

    public static Order Create(Guid customerId, decimal total)
    {
        if (total <= 0)
        {
            throw new ArgumentException("Total must be greater than zero", nameof(total));
        }

        return new Order()
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Total = total,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Confirm()
    {
        if (this.Status != OrderStatus.Pending)
        {
            throw new InvalidOperationException("Only pending orders can be confirmed");
        }

        this.Status = OrderStatus.Confirmed;
    }

    public void Reject()
    {
        if (this.Status == OrderStatus.Confirmed)
        {
            throw new InvalidOperationException("Confirmed orders cannot be rejected");
        }

        this.Status = OrderStatus.Rejected;
    }
}