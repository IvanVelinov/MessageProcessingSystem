
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Shared.Config;
using System.Text;

namespace MessageProcessingSystemAPI.Services;

/// <summary>
/// Service responsible for establishing connection to RabbitMQ and publishing messages.
/// </summary>
public class RabbitMqService : IAsyncDisposable
{
    private readonly RabbitMqSettings _settings;
    private IConnection? _connection;
    private IChannel? _channel;
    private bool _isInitialized;

    public RabbitMqService(IOptions<RabbitMqSettings> options)
    {
        _settings = options.Value;
    }

    /// <summary>
    /// Asynchronously creates a connection and channel to RabbitMQ, and declares the queue.
    /// </summary>
    public async Task StartAsync()
    {
        var factory = new ConnectionFactory
        {
            HostName = _settings.Host,
            UserName = _settings.Username,
            Password = _settings.Password
        };

        _connection = await factory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();

        await _channel.QueueDeclareAsync(
            queue: _settings.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null
        );

        _isInitialized = true;
    }

    /// <summary>
    /// Publishes a SHA1 message to RabbitMQ queue asynchronously.
    /// </summary>
    public async Task PublishAsync(string sha1)
    {
        if (!_isInitialized || _channel is null)
            throw new InvalidOperationException("RabbitMQ service not initialized.");

        var properties = new BasicProperties
        {
            DeliveryMode = DeliveryModes.Persistent // make the message persistent
        };

        var body = Encoding.UTF8.GetBytes(sha1);

        await _channel.BasicPublishAsync(
            exchange: "",
            routingKey: _settings.QueueName,
            mandatory: true,
            basicProperties: properties,
            body: body
        );
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
            await _channel.CloseAsync();

        if (_connection is not null)
            await _connection.CloseAsync();
    }
}
