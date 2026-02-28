using AssignmentOrderTracker.Domain;
using Microsoft.Extensions.Logging;

namespace AssignmentOrderTracker.Application;

public class OrderService(IOrderRepository repository, IOrderStatusTransitionPolicy transitionPolicy, ILogger<OrderService> logger) : IOrderService
{
    public async Task<Order> CreateAsync(Order order, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(order.OrderId)
            || string.IsNullOrWhiteSpace(order.CustomerName)
            || string.IsNullOrWhiteSpace(order.CustomerEmail)
            || string.IsNullOrWhiteSpace(order.CustomerMobile))
        {
            throw new ApiException("VALIDATION_ERROR", "orderId, customerName, customerEmail, and customerMobile are required");
        }

        if (order.Items.Count == 0)
        {
            throw new ApiException("VALIDATION_ERROR", "at least one item is required");
        }

        var existingOrder = await repository.GetByIdAsync(order.OrderId.Trim(), cancellationToken);
        if (existingOrder is not null)
        {
            throw new ApiException("DUPLICATE_ORDER", $"Order '{order.OrderId}' already exists", 409);
        }

        order.OrderId = order.OrderId.Trim();
        order.CustomerName = order.CustomerName.Trim();
        order.CustomerEmail = order.CustomerEmail.Trim();
        order.CustomerMobile = order.CustomerMobile.Trim();
        order.CurrentStatus = OrderStatus.Placed;
        order.Timeline.Clear();
        order.Notes.Clear();

        await repository.AddAsync(order, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Created order {OrderId} for {CustomerEmail}", order.OrderId, order.CustomerEmail);
        return order;
    }

    public async Task<List<Order>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ApiException("VALIDATION_ERROR", "query is required");
        }

        var results = await repository.SearchAsync(query.Trim(), cancellationToken);
        logger.LogInformation("Searched orders with query {Query} and received {Count} results", query, results.Count);
        return results;
    }

    public async Task<Order> GetDetailsAsync(string orderId, CancellationToken cancellationToken)
    {
        var order = await repository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new ApiException("ORDER_NOT_FOUND", $"Order '{orderId}' was not found", 404);
        return order;
    }

    public async Task<OrderNote> AddNoteAsync(string orderId, string author, string text, CancellationToken cancellationToken)
    {
        var order = await GetDetailsAsync(orderId, cancellationToken);

        if (string.IsNullOrWhiteSpace(author) || string.IsNullOrWhiteSpace(text))
            throw new ApiException("VALIDATION_ERROR", "author and text are required");

        if (text.Length > 500)
            throw new ApiException("VALIDATION_ERROR", "note text must be 500 characters or fewer");

        var note = new OrderNote { Author = author.Trim(), Text = text.Trim(), CreatedAt = DateTimeOffset.UtcNow };
        order.Notes.Add(note);
        await repository.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Added note to order {OrderId} by {Author}", orderId, author);
        return note;
    }

    public async Task<OrderTimelineEntry> UpdateStatusAsync(string orderId, OrderStatus newStatus, string changedBy, CancellationToken cancellationToken)
    {
        var order = await GetDetailsAsync(orderId, cancellationToken);

        if (order.CurrentStatus == newStatus)
        {
            return new OrderTimelineEntry
            {
                FromStatus = order.CurrentStatus,
                ToStatus = newStatus,
                ChangedBy = string.IsNullOrWhiteSpace(changedBy) ? "support" : changedBy,
                ChangedAt = DateTimeOffset.UtcNow
            };
        }

        if (!transitionPolicy.IsValid(order.CurrentStatus, newStatus))
        {
            throw new ApiException("INVALID_STATUS_TRANSITION", $"Cannot transition from {order.CurrentStatus} to {newStatus}");
        }

        var entry = new OrderTimelineEntry
        {
            FromStatus = order.CurrentStatus,
            ToStatus = newStatus,
            ChangedBy = string.IsNullOrWhiteSpace(changedBy) ? "support" : changedBy,
            ChangedAt = DateTimeOffset.UtcNow
        };

        order.CurrentStatus = newStatus;
        order.Timeline.Add(entry);
        await repository.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Updated order {OrderId} from {FromStatus} to {ToStatus}", orderId, entry.FromStatus, entry.ToStatus);
        return entry;
    }
}
