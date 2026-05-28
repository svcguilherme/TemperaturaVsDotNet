using Microsoft.EntityFrameworkCore;
using OrdersApi.Models;

namespace OrdersApi.Data;

public static class OrderSeeder
{
    public static async Task SeedAsync(OrdersDbContext db, int count = 10_000)
    {
        if (await db.Orders.AnyAsync()) return;

        var rng = new Random(42);
        var orders = Enumerable.Range(1, count).Select(i => new Order
        {
            OrderNumber = 100_000 + i,
            TotalValue = Math.Round((decimal)(rng.NextDouble() * 9990 + 10), 2),
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow.AddSeconds(-rng.Next(0, 86400))
        }).ToList();

        await db.Orders.AddRangeAsync(orders);
        await db.SaveChangesAsync();
    }
}
