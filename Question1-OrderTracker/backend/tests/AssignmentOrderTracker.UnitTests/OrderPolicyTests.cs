using AssignmentOrderTracker.Application;
using AssignmentOrderTracker.Domain;

namespace AssignmentOrderTracker.UnitTests;

public class OrderPolicyTests
{
    private readonly OrderStatusTransitionPolicy _policy = new();

    [Fact]
    public void Allows_Placed_To_Paid() => Assert.True(_policy.IsValid(OrderStatus.Placed, OrderStatus.Paid));

    [Fact]
    public void Rejects_Delivered_To_Cancelled() => Assert.False(_policy.IsValid(OrderStatus.Delivered, OrderStatus.Cancelled));

    [Fact]
    public void Rejects_Same_Status() => Assert.False(_policy.IsValid(OrderStatus.Paid, OrderStatus.Paid));
}
