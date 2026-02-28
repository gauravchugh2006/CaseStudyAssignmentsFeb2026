using AssignmentOrderTracker.Application;
using AssignmentOrderTracker.Domain;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace AssignmentOrderTracker.Api;

[ApiController]
[Route("api/orders")]
public class OrdersController(IOrderService service) : ControllerBase
{

    /// <summary>
    /// Creates a new order with customer details, totals, and line items.
    /// </summary>
    /// <param name="request">The order payload to create.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>The created order details.</returns>
    [HttpPost]
    public async Task<ActionResult<OrderDetailsDto>> Create(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var order = await service.CreateAsync(new Order
        {
            OrderId = request.OrderId,
            CustomerName = request.CustomerName,
            CustomerEmail = request.CustomerEmail,
            CustomerMobile = request.CustomerMobile,
            SubTotal = request.SubTotal,
            Tax = request.Tax,
            Shipping = request.Shipping,
            GrandTotal = request.GrandTotal,
            Items = request.Items.Select(i => new OrderItem { Name = i.Name, Quantity = i.Quantity, Price = i.Price }).ToList()
        }, cancellationToken);

        return CreatedAtAction(nameof(Get), new { orderId = order.OrderId }, new OrderDetailsDto(
            order.OrderId,
            order.CustomerName,
            order.CustomerEmail,
            order.CustomerMobile,
            order.CurrentStatus.ToString().ToUpperInvariant(),
            order.SubTotal,
            order.Tax,
            order.Shipping,
            order.GrandTotal,
            order.Items.Select(i => new ItemDto(i.Name, i.Quantity, i.Price)).ToList(),
            order.Timeline.Select(t => new TimelineDto(t.FromStatus.ToString().ToUpperInvariant(), t.ToStatus.ToString().ToUpperInvariant(), t.ChangedBy, t.ChangedAt)).ToList(),
            order.Notes.Select(n => new NoteDto(n.Author, n.Text, n.CreatedAt)).ToList()));
    }

    /// <summary>
    /// Searches orders by order ID, customer email, or mobile number.
    /// </summary>
    /// <param name="query">Search string used to find matching orders.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>A list of matching order summaries.</returns>
    [HttpGet]
    public async Task<ActionResult<List<OrderSummaryDto>>> Search([FromQuery] string query, CancellationToken cancellationToken)
    {
        var orders = await service.SearchAsync(query, cancellationToken);
        return orders.Select(o => new OrderSummaryDto(o.OrderId, o.CustomerEmail, o.CustomerMobile, o.CurrentStatus.ToString().ToUpperInvariant(), o.GrandTotal)).ToList();
    }

    /// <summary>
    /// Retrieves complete details for a single order.
    /// </summary>
    /// <param name="orderId">The unique order ID.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>The order details including items, timeline, and notes.</returns>
    [HttpGet("{orderId}")]
    public async Task<ActionResult<OrderDetailsDto>> Get(string orderId, CancellationToken cancellationToken)
    {
        var order = await service.GetDetailsAsync(orderId, cancellationToken);
        return new OrderDetailsDto(
            order.OrderId,
            order.CustomerName,
            order.CustomerEmail,
            order.CustomerMobile,
            order.CurrentStatus.ToString().ToUpperInvariant(),
            order.SubTotal,
            order.Tax,
            order.Shipping,
            order.GrandTotal,
            order.Items.Select(i => new ItemDto(i.Name, i.Quantity, i.Price)).ToList(),
            order.Timeline.Select(t => new TimelineDto(t.FromStatus.ToString().ToUpperInvariant(), t.ToStatus.ToString().ToUpperInvariant(), t.ChangedBy, t.ChangedAt)).ToList(),
            order.Notes.Select(n => new NoteDto(n.Author, n.Text, n.CreatedAt)).ToList());
    }

    /// <summary>
    /// Adds an internal note to an existing order.
    /// </summary>
    /// <param name="orderId">The unique order ID.</param>
    /// <param name="request">The note author and text.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>The created note.</returns>
    [HttpPost("{orderId}/notes")]
    public async Task<ActionResult<NoteDto>> AddNote(string orderId, AddNoteRequest request, CancellationToken cancellationToken)
    {
        var note = await service.AddNoteAsync(orderId, request.Author, request.Text, cancellationToken);
        return new NoteDto(note.Author, note.Text, note.CreatedAt);
    }

    /// <summary>
    /// Updates the current status of an order.
    /// </summary>
    /// <param name="orderId">The unique order ID.</param>
    /// <param name="request">Requested status change and actor metadata.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>The newly created timeline entry for the transition.</returns>
    [HttpPost("{orderId}/status")]
    public async Task<ActionResult<TimelineDto>> UpdateStatus(string orderId, UpdateStatusRequest request, CancellationToken cancellationToken)
    {
        var requestedStatus = request.NewStatusOrStatus;

        if (!Enum.TryParse<OrderStatus>(requestedStatus, true, out var newStatus))
        {
            throw new ApiException("VALIDATION_ERROR", "newStatus is invalid");
        }

        var timeline = await service.UpdateStatusAsync(orderId, newStatus, request.ChangedBy, cancellationToken);
        return new TimelineDto(timeline.FromStatus.ToString().ToUpperInvariant(), timeline.ToStatus.ToString().ToUpperInvariant(), timeline.ChangedBy, timeline.ChangedAt);
    }
}

public record OrderSummaryDto(string OrderId, string Email, string Mobile, string Status, decimal Total);
public record ItemDto(string Name, int Quantity, decimal Price);
public record TimelineDto(string FromStatus, string ToStatus, string ChangedBy, DateTimeOffset ChangedAt);
public record NoteDto(string Author, string Text, DateTimeOffset CreatedAt);
public record OrderDetailsDto(string OrderId, string CustomerName, string CustomerEmail, string CustomerMobile, string CurrentStatus, decimal SubTotal, decimal Tax, decimal Shipping, decimal GrandTotal, List<ItemDto> Items, List<TimelineDto> Timeline, List<NoteDto> Notes);
public record AddNoteRequest(string Author, string Text);
public record UpdateStatusRequest
{
    public string? NewStatus { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    public string ChangedBy { get; init; } = "support";

    [JsonIgnore]
    public string? NewStatusOrStatus => string.IsNullOrWhiteSpace(NewStatus) ? Status : NewStatus;
}
public record CreateOrderItemRequest(string Name, int Quantity, decimal Price);
public record CreateOrderRequest(string OrderId, string CustomerName, string CustomerEmail, string CustomerMobile, decimal SubTotal, decimal Tax, decimal Shipping, decimal GrandTotal, List<CreateOrderItemRequest> Items);
