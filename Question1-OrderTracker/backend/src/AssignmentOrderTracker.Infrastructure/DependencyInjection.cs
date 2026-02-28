using AssignmentOrderTracker.Application;
using AssignmentOrderTracker.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AssignmentOrderTracker.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddDbContext<OrderDbContext>(opt => opt.UseInMemoryDatabase("orders-db"));
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddSingleton<IOrderStatusTransitionPolicy, OrderStatusTransitionPolicy>();
        return services;
    }

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        if (await db.Orders.AnyAsync()) return;

        db.Orders.AddRange(
            new Order
            {
                OrderId = "ORD-1001",
                CustomerName = "Asha Mehta",
                CustomerEmail = "asha@example.com",
                CustomerMobile = "9876543210",
                SubTotal = 1200,
                Tax = 216,
                Shipping = 80,
                GrandTotal = 1496,
                CurrentStatus = OrderStatus.Paid,
                Items = [new OrderItem { Name = "DotNet Course", Quantity = 1, Price = 1200 }],
                Timeline = [new OrderTimelineEntry { FromStatus = OrderStatus.Placed, ToStatus = OrderStatus.Paid, ChangedBy = "system" }]
            },
            new Order
            {
                OrderId = "ORD-1002",
                CustomerName = "Rahul Jain",
                CustomerEmail = "rahul@example.com",
                CustomerMobile = "9000011111",
                SubTotal = 900,
                Tax = 162,
                Shipping = 50,
                GrandTotal = 1112,
                CurrentStatus = OrderStatus.Placed,
                Items = [new OrderItem { Name = "React Mastery", Quantity = 1, Price = 900 }]
            });

        await db.SaveChangesAsync();
    }
}
