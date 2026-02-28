namespace AssignmentOrderTracker.Domain;

public enum OrderStatus
{
    Placed,
    Paid,
    Shipped,
    Delivered,
    Cancelled
}

public class Order
{
    public string OrderId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerMobile { get; set; } = string.Empty;
    public decimal SubTotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Shipping { get; set; }
    public decimal GrandTotal { get; set; }
    public OrderStatus CurrentStatus { get; set; } = OrderStatus.Placed;
    public List<OrderItem> Items { get; set; } = [];
    public List<OrderTimelineEntry> Timeline { get; set; } = [];
    public List<OrderNote> Notes { get; set; } = [];
}

public class OrderItem
{
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

public class OrderTimelineEntry
{
    public OrderStatus FromStatus { get; set; }
    public OrderStatus ToStatus { get; set; }
    public string ChangedBy { get; set; } = "system";
    public DateTimeOffset ChangedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class OrderNote
{
    public string Author { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
