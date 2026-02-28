using AssignmentOrderTracker.Domain;

namespace AssignmentOrderTracker.Application;

public interface IOrderRepository
{
    Task<List<Order>> SearchAsync(string query, CancellationToken cancellationToken);
    Task<Order?> GetByIdAsync(string orderId, CancellationToken cancellationToken);
    Task AddAsync(Order order, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IOrderService
{
    Task<Order> CreateAsync(Order order, CancellationToken cancellationToken);
    Task<List<Order>> SearchAsync(string query, CancellationToken cancellationToken);
    Task<Order> GetDetailsAsync(string orderId, CancellationToken cancellationToken);
    Task<OrderNote> AddNoteAsync(string orderId, string author, string text, CancellationToken cancellationToken);
    Task<OrderTimelineEntry> UpdateStatusAsync(string orderId, OrderStatus newStatus, string changedBy, CancellationToken cancellationToken);
}

public interface IOrderStatusTransitionPolicy
{
    bool IsValid(OrderStatus from, OrderStatus to);
}

public sealed class ApiException(string code, string message, int statusCode = 400) : Exception(message)
{
    public string Code { get; } = code;
    public int StatusCode { get; } = statusCode;
}
