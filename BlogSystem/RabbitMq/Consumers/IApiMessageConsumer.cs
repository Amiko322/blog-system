using BlogSystem.Configuration.Constants;
using BlogSystem.Configuration.Options;
using BlogSystem.RabbitMq.Services;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace BlogSystem.RabbitMq.Consumers;

public interface IApiMessageConsumer
{
    void StartConsuming();

    void StopConsuming();
}

public class ApiMessageConsumer : IApiMessageConsumer, IDisposable
{
    private readonly ILogger<ApiMessageConsumer> _logger;
    private readonly IServiceProvider _sp;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly IModel _dlq;

    private EventingBasicConsumer? _consumer;
    private bool _isConsuming;

    public ApiMessageConsumer(
        IServiceProvider sp,
        ILogger<ApiMessageConsumer> logger,
        IOptions<RabbitMqOptions> options)
    {
        _sp = sp;
        _logger = logger;

        RabbitMqOptions _options = options.Value;

        _logger.LogInformation(
            "Initializing RabbitMQ consumer. Host={Host}:{Port}, VHost={VHost}, User={User}, Queue={Queue}, DLQ={DlqQueue}",
            _options.HostName,
            _options.Port,
            _options.VirtualHost, 
            _options.UserName,
            RabbitMqTopology.API_REQUEST_QUEUE,
            RabbitMqTopology.DEAD_LETTER_QUEUE);

        ConnectionFactory factory = new()
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost,
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _dlq = _connection.CreateModel();

        // DLQ
        _dlq.ExchangeDeclare(
            exchange: RabbitMqTopology.DEAD_LETTER_EXCHANGE,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false);

        _dlq.QueueDeclare(
            queue: RabbitMqTopology.DEAD_LETTER_QUEUE,
            durable: true,
            exclusive: false,
            autoDelete: false);

        _dlq.QueueBind(
            queue: RabbitMqTopology.DEAD_LETTER_QUEUE,
            exchange: RabbitMqTopology.DEAD_LETTER_EXCHANGE,
            routingKey: RabbitMqTopology.DEAD_LETTER_ROUTING_KEY);

        _logger.LogInformation(
            "DLQ configured. Exchange={Exchange}, Queue={Queue}, RoutingKey={RoutingKey}",
            RabbitMqTopology.DEAD_LETTER_EXCHANGE,
            RabbitMqTopology.DEAD_LETTER_QUEUE,
            RabbitMqTopology.DEAD_LETTER_ROUTING_KEY);


        // Основная очередь запросов
        Dictionary<string, object> args = new()
        {
            { "x-dead-letter-exchange", RabbitMqTopology.DEAD_LETTER_EXCHANGE },
            { "x-dead-letter-routing-key", RabbitMqTopology.DEAD_LETTER_ROUTING_KEY },
            { "x-message-ttl", 300000 }, // 5 минут
        };

        _channel.QueueDeclare(
            queue: RabbitMqTopology.API_REQUEST_QUEUE,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: args);

        _logger.LogInformation(
            "Request queue configured. Queue={Queue}, TTLms={TTLms}, DLX={DLX}, DLRK={DLRK}",
            RabbitMqTopology.API_REQUEST_QUEUE,
            300000,
            RabbitMqTopology.DEAD_LETTER_EXCHANGE,
            RabbitMqTopology.DEAD_LETTER_ROUTING_KEY);
    }

    public void StartConsuming()
    {
        if (_isConsuming)
            return;

        _consumer = new EventingBasicConsumer(_channel);
        _consumer.Received += async (_, ea) =>
        {
            using IServiceScope scope = _sp.CreateScope();

            IMessageProcessingService processor = scope.ServiceProvider
                .GetRequiredService<IMessageProcessingService>();

            await processor.ProcessMessageAsync(ea, _sp, _channel);
        };

        _channel.BasicConsume(
            queue: RabbitMqTopology.API_REQUEST_QUEUE,
            autoAck: false,
            consumer: _consumer);
        _isConsuming = true;

        _logger.LogInformation(
            "Consumer started. Queue={Queue}, AutoAck={AutoAck}",
            RabbitMqTopology.API_REQUEST_QUEUE,
            false);
    }

    public void StopConsuming()
    {
        if (!_isConsuming || _consumer is null)
        {
            _logger.LogWarning("StopConsuming ignored: consumer not running");
            return;
        }

        _channel.BasicCancel(_consumer.ConsumerTags.First());
        _isConsuming = false;

        _logger.LogInformation(
            "Consumer stopped. Queue={Queue}", 
            RabbitMqTopology.API_REQUEST_QUEUE);
    }

    public void Dispose()
    {
        StopConsuming();
        _channel?.Close();
        _dlq?.Close();
        _connection?.Close();
    }
}
