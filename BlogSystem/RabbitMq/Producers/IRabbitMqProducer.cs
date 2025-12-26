using BlogSystem.Configuration.Constants;
using BlogSystem.Configuration.Options;
using BlogSystem.RabbitMq.Models;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace BlogSystem.RabbitMq.Producers;

public interface IRabbitMqProducer
{
    Task<StandardResponseMessage> SendAsync(StandardRequestMessage request);
}

public class RabbitMqProducer : IRabbitMqProducer, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public RabbitMqProducer(IOptions<RabbitMqOptions> options)
    {
        RabbitMqOptions _options = options.Value;

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

        var args = new Dictionary<string, object>
        {
            { "x-dead-letter-exchange", "dead_letter_exchange" },
            { "x-dead-letter-routing-key", "dead.letter" },
            { "x-message-ttl", 300000 },
        };

        _channel.QueueDeclare(
            queue: RabbitMqTopology.API_REQUEST_QUEUE,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: args);

        _channel.QueueDeclare(
            queue: RabbitMqTopology.API_RESPONSE_QUEUE,
            durable: true,
            exclusive: false,
            autoDelete: false);
    }

    public Task<StandardResponseMessage> SendAsync(StandardRequestMessage request)
    {
        TaskCompletionSource<StandardResponseMessage> tcs = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        string correlationId = request.Id.ToString();

        var replyQueue = _channel.QueueDeclare(
            queue: string.Empty,
            durable: false,
            exclusive: true,
            autoDelete: true,
            arguments: null).QueueName;

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += (_, ea) =>
        {
            try
            {
                var incomingCorr = ea.BasicProperties?.CorrelationId;
                if (incomingCorr != correlationId)
                {
                    _channel.BasicAck(ea.DeliveryTag, false);
                    return;
                }

                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var response = JsonSerializer.Deserialize<StandardResponseMessage>(json);

                _channel.BasicAck(ea.DeliveryTag, false);

                if (response is null)
                    tcs.TrySetException(new Exception("Empty response"));
                else
                    tcs.TrySetResult(response);
            }
            catch (Exception ex)
            {
                _channel.BasicAck(ea.DeliveryTag, false);
                tcs.TrySetException(ex);
            }
        };

        var consumerTag = _channel.BasicConsume(replyQueue, autoAck: false, consumer: consumer);

        var jsonReq = JsonSerializer.Serialize(request);
        var body = Encoding.UTF8.GetBytes(jsonReq);

        var props = _channel.CreateBasicProperties();
        props.Persistent = true;
        props.CorrelationId = correlationId;
        props.ReplyTo = replyQueue;

        _channel.BasicPublish(
            exchange: string.Empty,
            routingKey: RabbitMqTopology.API_REQUEST_QUEUE,
            basicProperties: props,
            body: body);

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(10));
                if (!tcs.Task.IsCompleted)
                    tcs.TrySetException(new TimeoutException("Timeout waiting reply queue"));
            }
            finally
            {
                _channel.BasicCancel(consumerTag);
                _channel.QueueDelete(replyQueue);
            }
        });

        return tcs.Task;
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }
}