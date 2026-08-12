namespace CloudOrders.Domain;

public class Order
{
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public decimal Total => _items.Sum(x => x.Subtotal);
    public OrderStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    private readonly List<OrderItem> _items = [];
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    private Order()
    {
        Status = OrderStatus.Pending;
    }

    public static Order Create(Guid customerId, IEnumerable<OrderItem> items)
    {
        if (customerId == Guid.Empty)
        {
            throw new ArgumentException("Customer id cannot be empty", nameof(customerId));
        }

        ArgumentNullException.ThrowIfNull(items);

        var itemList = items.ToList();

        if (itemList.Count == 0)
        {
            throw new ArgumentException("Order must have at least one item", nameof(items));
        }

        var newOrder = new Order()
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        newOrder._items.AddRange(items);

        return newOrder;
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