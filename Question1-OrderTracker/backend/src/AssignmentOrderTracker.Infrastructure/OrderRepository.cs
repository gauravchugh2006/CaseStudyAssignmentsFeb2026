using AssignmentOrderTracker.Application;
using AssignmentOrderTracker.Domain;
using Microsoft.EntityFrameworkCore;

namespace AssignmentOrderTracker.Infrastructure;

public class OrderRepository(OrderDbContext dbContext) : IOrderRepository
{
    public Task<Order?> GetByIdAsync(string orderId, CancellationToken cancellationToken) =>
        dbContext.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId, cancellationToken);

    public Task AddAsync(Order order, CancellationToken cancellationToken) =>
        dbContext.Orders.AddAsync(order, cancellationToken).AsTask();

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);

    public async Task<List<Order>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        var orderById = await dbContext.Orders
            .Where(o => o.OrderId.Equals(query))
            .ToListAsync(cancellationToken);

        if (orderById.Count != 0)
            return orderById;

        query = query.ToLowerInvariant();

        return await dbContext.Orders
            .Where(o => o.CustomerEmail.ToLower().Contains(query) || o.CustomerMobile.Contains(query))
            .ToListAsync(cancellationToken);
    }
}
