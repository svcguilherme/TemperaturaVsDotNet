using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace OrdersApi.Services;

/// <summary>
/// Simula Azure Service Bus: direct exchange com fila durável para comandos de processamento de pedidos.
/// Producer enfileira IDs; consumer (OrderProcessorWorker) processa com ack manual.
/// </summary>
public class ServiceBusService : IDisposable
{
    public const string Exchange = "orders.servicebus";
    public const string Queue = "order-processing";
    public const string RoutingKey = "order.process";

    private IConnection? _connection;
    private IModel? _channel;
    private readonly IConfiguration _config;
    private readonly ILogger<ServiceBusService> _logger;

    public ServiceBusService(IConfiguration config, ILogger<ServiceBusService> logger)
    {
        _config = config;
        _logger = logger;
        TryConnect();
    }

    private void TryConnect()
    {
        try
        {
            var factory = new ConnectionFactory
            {
                Uri = new Uri(_config["RABBITMQ_URL"] ?? "amqp://guest:guest@localhost:5672"),
                RequestedConnectionTimeout = TimeSpan.FromSeconds(5),
                AutomaticRecoveryEnabled = true
            };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.ExchangeDeclare(Exchange, ExchangeType.Direct, durable: true);
            _channel.QueueDeclare(Queue, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind(Queue, Exchange, RoutingKey);
            _channel.BasicQos(0, 50, false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("ServiceBus (RabbitMQ) unavailable: {msg}", ex.Message);
        }
    }

    public bool IsAvailable => _channel?.IsOpen == true;

    public void EnqueueOrder(int orderId)
    {
        if (_channel == null) return;
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { orderId }));
        var props = _channel.CreateBasicProperties();
        props.Persistent = true;
        props.ContentType = "application/json";
        _channel.BasicPublish(Exchange, RoutingKey, props, body);
    }

    public void EnqueueBatch(IEnumerable<int> orderIds)
    {
        if (_channel == null) return;
        var batch = _channel.CreateBasicPublishBatch();
        foreach (var id in orderIds)
        {
            var body = new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { orderId = id })));
            var props = _channel.CreateBasicProperties();
            props.Persistent = true;
            batch.Add(Exchange, RoutingKey, false, props, body);
        }
        batch.Publish();
    }

    public IModel? GetChannel() => _channel;

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }
}
