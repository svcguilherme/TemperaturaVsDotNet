using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace PersonApi.Services;

public class MessagingService : IDisposable
{
    private readonly IConnection? _connection;
    private readonly IModel? _channel;

    public MessagingService(IConfiguration config)
    {
        try
        {
            var factory = new ConnectionFactory
            {
                Uri = new Uri(config["RABBITMQ_URL"] ?? "amqp://guest:guest@localhost:5672"),
                RequestedConnectionTimeout = TimeSpan.FromSeconds(5)
            };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.ExchangeDeclare("person-events", ExchangeType.Topic, durable: true);
        }
        catch { }
    }

    public Task PublishAsync(string exchange, string routingKey, object payload)
    {
        if (_channel == null) return Task.CompletedTask;
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
        var props = _channel.CreateBasicProperties();
        props.Persistent = true;
        _channel.BasicPublish(exchange, routingKey, props, body);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }
}
