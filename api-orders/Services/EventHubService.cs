using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace OrdersApi.Services;

/// <summary>
/// Simula Azure Event Hub: fanout exchange que broadcast status de pedidos para múltiplos consumers.
/// Filas: audit-events, analytics-events, notification-events (consumer groups).
/// </summary>
public class EventHubService : IDisposable
{
    public const string Exchange = "orders.eventhub";

    private IConnection? _connection;
    private IModel? _channel;
    private readonly ILogger<EventHubService> _logger;

    public EventHubService(IConfiguration config, ILogger<EventHubService> logger)
    {
        _logger = logger;
        try
        {
            var factory = new ConnectionFactory
            {
                Uri = new Uri(config["RABBITMQ_URL"] ?? "amqp://guest:guest@localhost:5672"),
                RequestedConnectionTimeout = TimeSpan.FromSeconds(5),
                AutomaticRecoveryEnabled = true
            };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Fanout exchange — todos os consumers recebem todos os eventos
            _channel.ExchangeDeclare(Exchange, ExchangeType.Fanout, durable: true);

            // Consumer groups (simulando Event Hub consumer groups)
            foreach (var queue in new[] { "audit-events", "analytics-events", "notification-events" })
            {
                _channel.QueueDeclare(queue, durable: true, exclusive: false, autoDelete: false);
                _channel.QueueBind(queue, Exchange, "");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("EventHub (RabbitMQ) unavailable: {msg}", ex.Message);
        }
    }

    public void PublishStatusChanged(int orderId, int orderNumber, string status, decimal totalValue)
    {
        if (_channel == null) return;

        var payload = new
        {
            eventType = "order.status.changed",
            orderId,
            orderNumber,
            status,
            totalValue,
            timestamp = DateTime.UtcNow
        };

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
        var props = _channel.CreateBasicProperties();
        props.Persistent = true;
        props.ContentType = "application/json";
        props.Headers = new Dictionary<string, object> { ["status"] = status };

        _channel.BasicPublish(Exchange, "", props, body);
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }
}
