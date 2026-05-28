using Microsoft.EntityFrameworkCore;
using OrdersApi.Data;
using OrdersApi.Models;
using Prometheus;

namespace OrdersApi.Services;

public class OrderService(
    OrdersDbContext db,
    ServiceBusService serviceBus,
    ILogger<OrderService> logger)
{
    private static readonly Counter OrdersEnqueued = Metrics
        .CreateCounter("orders_enqueued_total", "Total de pedidos enfileirados para processamento");

    public async Task<(int total, int enqueued)> EnqueuePendingOrdersAsync()
    {
        var pendingIds = await db.Orders
            .Where(o => o.Status == OrderStatus.Pending)
            .Select(o => o.Id)
            .ToListAsync();

        if (pendingIds.Count == 0)
            return (0, 0);

        serviceBus.EnqueueBatch(pendingIds);
        OrdersEnqueued.Inc(pendingIds.Count);

        logger.LogInformation("Enqueued {count} pending orders to Service Bus", pendingIds.Count);
        return (pendingIds.Count, pendingIds.Count);
    }

    public async Task<object> GetStatsAsync()
    {
        var total = await db.Orders.CountAsync();
        var counts = await db.Orders
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var byStatus = counts.Select(c => new
        {
            status = c.Status.ToString(),
            count = c.Count
        }).ToList();

        return new { total, byStatus };
    }

    public async Task<List<Order>> GetPagedAsync(int page, int pageSize, string? status)
    {
        var query = db.Orders.AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<OrderStatus>(status, true, out var s))
            query = query.Where(o => o.Status == s);

        return await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetTotalCountAsync(string? status)
    {
        var query = db.Orders.AsQueryable();
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<OrderStatus>(status, true, out var s))
            query = query.Where(o => o.Status == s);
        return await query.CountAsync();
    }

    public async Task ResetAllOrdersAsync()
    {
        await db.Orders.ExecuteUpdateAsync(s =>
            s.SetProperty(o => o.Status, OrderStatus.Pending)
             .SetProperty(o => o.ProcessedAt, (DateTime?)null)
             .SetProperty(o => o.ErrorMessage, (string?)null));

        logger.LogInformation("All orders reset to Pending");
    }
}
