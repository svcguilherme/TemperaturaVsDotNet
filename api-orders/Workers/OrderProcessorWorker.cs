using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrdersApi.Data;
using OrdersApi.Models;
using OrdersApi.Services;
using Prometheus;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OrdersApi.Workers;

/// <summary>
/// Background worker que consome mensagens do Service Bus (RabbitMQ direct queue)
/// e processa pedidos, simulando latência e falha aleatória.
/// Cada mudança de status é publicada no Event Hub (fanout exchange).
/// </summary>
public class OrderProcessorWorker(
    IServiceScopeFactory scopeFactory,
    ServiceBusService serviceBus,
    EventHubService eventHub,
    ILogger<OrderProcessorWorker> logger) : BackgroundService
{
    private static readonly Counter OrdersProcessed = Metrics
        .CreateCounter("orders_processed_total", "Total de pedidos processados", labelNames: ["status"]);

    private static readonly Histogram ProcessingTime = Metrics
        .CreateHistogram("order_processing_duration_seconds", "Tempo de processamento por pedido",
            new HistogramConfiguration { Buckets = Histogram.LinearBuckets(0.01, 0.05, 20) });

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("OrderProcessorWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            var channel = serviceBus.GetChannel();
            if (channel == null || !channel.IsOpen)
            {
                await Task.Delay(5000, stoppingToken);
                continue;
            }

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += async (_, ea) =>
            {
                using var timer = ProcessingTime.NewTimer();
                try
                {
                    var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                    var msg = JsonDocument.Parse(body).RootElement;
                    var orderId = msg.GetProperty("orderId").GetInt32();

                    await ProcessOrderAsync(orderId, channel, ea.DeliveryTag);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing order message");
                    channel.BasicNack(ea.DeliveryTag, false, requeue: true);
                }
            };

            channel.BasicConsume(ServiceBusService.Queue, autoAck: false, consumer: consumer);

            // Aguarda até cancelamento
            try { await Task.Delay(Timeout.Infinite, stoppingToken); }
            catch (OperationCanceledException) { }
        }
    }

    private async Task ProcessOrderAsync(int orderId, IModel channel, ulong deliveryTag)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();

        var order = await db.Orders.FindAsync(orderId);
        if (order == null)
        {
            channel.BasicAck(deliveryTag, false);
            return;
        }

        // Marca como Processing
        order.Status = OrderStatus.Processing;
        await db.SaveChangesAsync();
        eventHub.PublishStatusChanged(order.Id, order.OrderNumber, "Processing", order.TotalValue);

        // Simula trabalho (10ms a 200ms)
        await Task.Delay(Random.Shared.Next(10, 200));

        // 10% de falha aleatória
        if (Random.Shared.NextDouble() < 0.10)
        {
            order.Status = OrderStatus.Failed;
            order.ProcessedAt = DateTime.UtcNow;
            order.ErrorMessage = "Falha simulada no processamento";
            OrdersProcessed.WithLabels("failed").Inc();
            eventHub.PublishStatusChanged(order.Id, order.OrderNumber, "Failed", order.TotalValue);
            logger.LogWarning("Order {num} failed", order.OrderNumber);
        }
        else
        {
            order.Status = OrderStatus.Completed;
            order.ProcessedAt = DateTime.UtcNow;
            OrdersProcessed.WithLabels("completed").Inc();
            eventHub.PublishStatusChanged(order.Id, order.OrderNumber, "Completed", order.TotalValue);
        }

        await db.SaveChangesAsync();
        channel.BasicAck(deliveryTag, false);
    }
}
