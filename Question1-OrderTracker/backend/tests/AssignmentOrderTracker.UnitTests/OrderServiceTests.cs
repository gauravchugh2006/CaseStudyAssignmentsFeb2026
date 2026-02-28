using AssignmentOrderTracker.Application;
using AssignmentOrderTracker.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AssignmentOrderTracker.UnitTests;

public class OrderServiceTests
{

    [Fact]
    public async Task Creates_Order_With_Placed_Status()
    {
        var repository = new Mock<IOrderRepository>();
        repository.Setup(x => x.GetByIdAsync("ORD-2001", It.IsAny<CancellationToken>())).ReturnsAsync((Order?)null);

        var service = new OrderService(repository.Object, new OrderStatusTransitionPolicy(), NullLogger<OrderService>.Instance);
        var order = new Order
        {
            OrderId = "ORD-2001",
            CustomerName = "Test User",
            CustomerEmail = "test@example.com",
            CustomerMobile = "9999999999",
            SubTotal = 100,
            Tax = 18,
            Shipping = 10,
            GrandTotal = 128,
            Items = [new OrderItem { Name = "Item A", Quantity = 1, Price = 100 }]
        };

        var created = await service.CreateAsync(order, CancellationToken.None);

        Assert.Equal(OrderStatus.Placed, created.CurrentStatus);
        repository.Verify(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Rejects_Note_Above_500_Characters()
    {
        var repository = new Mock<IOrderRepository>();
        repository.Setup(x => x.GetByIdAsync("ORD-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Order { OrderId = "ORD-1" });

        var service = new OrderService(repository.Object, new OrderStatusTransitionPolicy(), NullLogger<OrderService>.Instance);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            service.AddNoteAsync("ORD-1", "agent", new string('x', 501), CancellationToken.None));

        Assert.Equal("VALIDATION_ERROR", ex.Code);
    }

    [Fact]
    public async Task Adds_Timeline_On_Valid_Status_Transition()
    {
        var order = new Order { OrderId = "ORD-1", CurrentStatus = OrderStatus.Placed };
        var repository = new Mock<IOrderRepository>();
        repository.Setup(x => x.GetByIdAsync("ORD-1", It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var service = new OrderService(repository.Object, new OrderStatusTransitionPolicy(), NullLogger<OrderService>.Instance);

        var timeline = await service.UpdateStatusAsync("ORD-1", OrderStatus.Paid, "agent", CancellationToken.None);

        Assert.Equal(OrderStatus.Paid, order.CurrentStatus);
        Assert.Single(order.Timeline);
        Assert.Equal(OrderStatus.Placed, timeline.FromStatus);
    }
}
