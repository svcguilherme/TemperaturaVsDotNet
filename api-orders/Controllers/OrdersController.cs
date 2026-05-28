using Microsoft.AspNetCore.Mvc;
using OrdersApi.Services;

namespace OrdersApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController(OrderService orderService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? status = null)
    {
        pageSize = Math.Clamp(pageSize, 1, 200);
        var orders = await orderService.GetPagedAsync(page, pageSize, status);
        var total = await orderService.GetTotalCountAsync(status);

        return Ok(new
        {
            data = orders,
            pagination = new { page, pageSize, total, totalPages = (int)Math.Ceiling(total / (double)pageSize) }
        });
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats() =>
        Ok(await orderService.GetStatsAsync());

    [HttpPost("process")]
    public async Task<IActionResult> ProcessOrders()
    {
        var (total, enqueued) = await orderService.EnqueuePendingOrdersAsync();
        return Ok(new
        {
            message = $"{enqueued} pedidos enfileirados no Service Bus para processamento",
            total,
            enqueued
        });
    }

    [HttpPost("reset")]
    public async Task<IActionResult> Reset()
    {
        await orderService.ResetAllOrdersAsync();
        return Ok(new { message = "Todos os pedidos resetados para Pending" });
    }

    [HttpGet("/health")]
    public IActionResult Health() => Ok(new { status = "healthy", service = "api-orders", timestamp = DateTime.UtcNow });
}
