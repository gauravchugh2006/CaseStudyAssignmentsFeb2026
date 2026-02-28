using AssignmentOrderTracker.Application;
using AssignmentOrderTracker.Domain;

namespace AssignmentOrderTracker.UnitTests;

public class OrderStatusTransitionPolicyTests
{
    private readonly OrderStatusTransitionPolicy _policy = new();

    [Fact]
    public void Allows_Forward_Progression_When_Skipping_Intermediate_Status()
    {
        var isValid = _policy.IsValid(OrderStatus.Placed, OrderStatus.Shipped);

        Assert.True(isValid);
    }

    [Fact]
    public void Rejects_Backward_Transition()
    {
        var isValid = _policy.IsValid(OrderStatus.Shipped, OrderStatus.Paid);

        Assert.False(isValid);
    }
}
