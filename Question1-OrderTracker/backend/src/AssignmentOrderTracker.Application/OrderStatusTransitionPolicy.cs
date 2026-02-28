using AssignmentOrderTracker.Domain;

namespace AssignmentOrderTracker.Application;

public class OrderStatusTransitionPolicy : IOrderStatusTransitionPolicy
{
    private static readonly Dictionary<OrderStatus, int> ProgressionRank = new()
    {
        [OrderStatus.Placed] = 0,
        [OrderStatus.Paid] = 1,
        [OrderStatus.Shipped] = 2,
        [OrderStatus.Delivered] = 3
    };

    public bool IsValid(OrderStatus from, OrderStatus to)
    {
        if (from == to) return false;

        // Cancellation is terminal and can be triggered from any in-flight state.
        if (to == OrderStatus.Cancelled)
        {
            return from is not OrderStatus.Delivered and not OrderStatus.Cancelled;
        }

        // Delivered and Cancelled are terminal states and cannot move forward.
        if (from is OrderStatus.Delivered or OrderStatus.Cancelled)
        {
            return false;
        }

        // Allow forward progression without requiring intermediate API calls.
        return ProgressionRank.TryGetValue(from, out var fromRank)
            && ProgressionRank.TryGetValue(to, out var toRank)
            && toRank > fromRank;
    }
}
