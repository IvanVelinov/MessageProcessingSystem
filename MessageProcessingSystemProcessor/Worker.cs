using MessageProcessingSystemProcessor.Services;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Config;
using System.Text;

namespace MessageProcessingSystemProcessor
{
    /// <summary>
    /// Worker service that consumes SHA1 hashes from RabbitMQ and saves them into MariaDB.
    /// </summary>
    public class Worker : BackgroundService, IAsyncDisposable
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceProvider _services;
        private readonly RabbitMqSettings _settings;
        private IConnection _connection;
        private IChannel? _channel;

        // Buffer for temporarily storing incoming hashes before batch insert
        private readonly List<string> _buffer = new();
        // Lock object to ensure thread-safe access to the buffer
        private readonly object _lock = new();
        // Number of hashes to collect before triggering a batch insert
        private const int BatchSize = 100;

        public Worker(ILogger<Worker> logger, IServiceProvider services, IOptions<RabbitMqSettings> options)
        {
            _logger = logger;
            _services = services;
            _settings = options.Value;
        }

        /// <summary>
        /// Initializes RabbitMQ connection and channel, and declares the queue.
        /// </summary>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            var factory = new ConnectionFactory
            {
                HostName = _settings.Host,
                UserName = _settings.Username,
                Password = _settings.Password
            };

            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();
            await _channel.BasicQosAsync(0, 4, false); // Parallelism control

            await _channel.QueueDeclareAsync(queue: _settings.QueueName,
                                  durable: true,
                                  exclusive: false,
                                  autoDelete: false,
                                  arguments: null);

             await base.StartAsync(cancellationToken);
        }

        /// <summary>
        /// Consumes messages from RabbitMQ and saves them into the database.
        /// </summary>
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (model, ea) =>
            {
                var sha1 = Encoding.UTF8.GetString(ea.Body.ToArray());

                // Add incoming hash to the buffer with thread safety
                lock (_lock)
                {
                    _buffer.Add(sha1);
                }

                if (_buffer.Count >= BatchSize)
                {
                    List<string> batch;

                    lock (_lock)
                    {
                        batch = new List<string>(_buffer);
                        _buffer.Clear();
                    }

                    try
                    {
                        // Save the batch of hashes inside a transaction
                        // After saving, acknowledge the latest message to RabbitMQ
                        using var scope = _services.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<HashInsertService>();
                        await db.SaveHashBatchAsync(batch);

                        // Successfully saved all batch hashes; acknowledge the latest message
                        await _channel.BasicAckAsync(ea.DeliveryTag, multiple: true);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process batch of hashes.");

                        // Failed saving batch; negatively acknowledge the message
                        await _channel.BasicNackAsync(ea.DeliveryTag, multiple: true, requeue: false);
                    }
                }
                else
                {
                    // Acknowledge immediately if batch is not full yet
                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                }
            };

            _channel.BasicConsumeAsync(
                queue: _settings.QueueName,
                autoAck: false,
                consumer: consumer
            );

            return Task.CompletedTask;
        }

        public async ValueTask DisposeAsync()
        {
            if (_channel is not null)
                await _channel.CloseAsync();
            if (_connection is not null)
                await _connection.CloseAsync();
        }
    }
}
