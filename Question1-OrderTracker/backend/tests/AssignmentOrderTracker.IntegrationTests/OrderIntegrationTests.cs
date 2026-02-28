using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using AssignmentOrderTracker.Api;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace AssignmentOrderTracker.IntegrationTests;

public class OrderIntegrationTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();


    [Fact]
    public async Task Create_Order_Then_Get_Order_Returns_Created_Order()
    {
        var response = await _client.PostAsJsonAsync("/api/orders", new
        {
            orderId = "ORD-2001",
            customerName = "New Customer",
            customerEmail = "newcustomer@example.com",
            customerMobile = "9111111111",
            subTotal = 500,
            tax = 90,
            shipping = 20,
            grandTotal = 610,
            items = new[] { new { name = "Angular Course", quantity = 1, price = 500 } }
        });

        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<OrderDetailsDto>();
        Assert.NotNull(created);
        Assert.Equal("PLACED", created!.CurrentStatus);

        var searchResults = await _client.GetFromJsonAsync<List<OrderSummaryDto>>("/api/orders?query=newcustomer@example.com");
        Assert.NotNull(searchResults);
        Assert.Contains(searchResults!, order => order.OrderId == "ORD-2001");
    }

    [Fact]
    public async Task Add_Note_Then_Get_Order_Returns_Note()
    {
        var response = await _client.PostAsJsonAsync("/api/orders/ORD-1001/notes", new { author = "support", text = "Customer called" });
        response.EnsureSuccessStatusCode();

        var order = await _client.GetFromJsonAsync<OrderDetailsDto>("/api/orders/ORD-1001");
        Assert.NotNull(order);
        Assert.Contains(order!.Notes, n => n.Text == "Customer called");
    }

    [Fact]
    public async Task Update_Status_Then_Get_Order_Returns_Timeline()
    {
        var response = await _client.PostAsJsonAsync("/api/orders/ORD-1002/status", new { newStatus = "PAID", changedBy = "support" });
        response.EnsureSuccessStatusCode();

        var order = await _client.GetFromJsonAsync<OrderDetailsDto>("/api/orders/ORD-1002");
        Assert.NotNull(order);
        Assert.Equal("PAID", order!.CurrentStatus);
        Assert.Contains(order.Timeline, t => t.ToStatus == "PAID");
    }

    [Fact]
    public async Task Update_Status_With_Status_Field_Returns_Timeline()
    {
        var response = await _client.PostAsJsonAsync("/api/orders/ORD-1002/status", new { status = "PAID", changedBy = "support" });
        response.EnsureSuccessStatusCode();

        var timeline = await response.Content.ReadFromJsonAsync<TimelineDto>();
        Assert.NotNull(timeline);
        Assert.Equal("PAID", timeline!.ToStatus);
    }
}
