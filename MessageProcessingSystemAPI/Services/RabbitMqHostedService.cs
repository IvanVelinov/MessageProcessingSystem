
namespace MessageProcessingSystemAPI.Services
{
    /// <summary>
    /// BackgroundService to initialize RabbitMqService asynchronously on app startup.
    /// </summary>
    public class RabbitMqHostedService : BackgroundService
    {
        private readonly RabbitMqService _rabbitService;

        public RabbitMqHostedService(RabbitMqService rabbitService)
        {
            _rabbitService = rabbitService;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            await _rabbitService.StartAsync();
            await base.StartAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // No background task needed after startup.
            return Task.CompletedTask;
        }
    }
}
