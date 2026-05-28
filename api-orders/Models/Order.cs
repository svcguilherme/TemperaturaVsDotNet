using System.ComponentModel.DataAnnotations;

namespace OrdersApi.Models;

public class Order
{
    [Key]
    public int Id { get; set; }
    public int OrderNumber { get; set; }
    public decimal TotalValue { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public string? ErrorMessage { get; set; }
}

public enum OrderStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}
