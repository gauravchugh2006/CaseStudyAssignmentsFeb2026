using AssignmentOrderTracker.Domain;

namespace AssignmentOrderTracker.Application;

public class OrderStatusTransitionPolicy : IOrderStatusTransitionPolicy
{
    private static readonly Dictionary<OrderStatus, HashSet<OrderStatus>> Allowed = new()
    {
        [OrderStatus.Placed] = [OrderStatus.Paid, OrderStatus.Cancelled],
        [OrderStatus.Paid] = [OrderStatus.Shipped, OrderStatus.Cancelled],
        [OrderStatus.Shipped] = [OrderStatus.Delivered, OrderStatus.Cancelled],
        [OrderStatus.Delivered] = [],
        [OrderStatus.Cancelled] = []
    };

    public bool IsValid(OrderStatus from, OrderStatus to)
    {
        if (from == to) return false;
        return Allowed.TryGetValue(from, out var allowed) && allowed.Contains(to);
    }
}
