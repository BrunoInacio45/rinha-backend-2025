using RinhaBackend2025.Infra.Clients;
using StackExchange.Redis;

namespace RinhaBackend2025.Infra.Worker
{
    public class HealthProcessorWorker : BackgroundService
    {
        private readonly ProcessorDefaultClient _processorDefaultHttpClient;
        private readonly ProcessorFallbackClient _processorFallbackHttpClient;
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<PaymentWorker> _logger;
        private const int _secToRetry = 10;

        public HealthProcessorWorker(
            ProcessorDefaultClient processorDefaultHttpClient,
            ProcessorFallbackClient processorFallbackHttpClient,
            IConnectionMultiplexer redis,
            ILogger<PaymentWorker> logger)
        {
            _processorDefaultHttpClient = processorDefaultHttpClient;
            _processorFallbackHttpClient = processorFallbackHttpClient;
            _redis = redis;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var redisDb = _redis.GetDatabase();
            while (!stoppingToken.IsCancellationRequested)
            {
                await CheckProcessorDefault(redisDb, stoppingToken);
                await CheckProcessorFallback(redisDb, stoppingToken);
                await Task.Delay(TimeSpan.FromMinutes(_secToRetry), stoppingToken);
            }
        }

        private async Task CheckProcessorDefault(IDatabase redisDb, CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation("Checking processor default health at: {time}", DateTimeOffset.Now);
                var healthy = await _processorDefaultHttpClient.CheckHealthAsync(stoppingToken);
                await redisDb.StringSetAsync("processor:default:health", healthy);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to check processor default health at: {ex.Message}", DateTimeOffset.Now);
                await redisDb.StringSetAsync("processor:default:health", false);
            }
        }

        private async Task CheckProcessorFallback(IDatabase redisDb, CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation("Checking processor fallback health at: {time}", DateTimeOffset.Now);
                var healthy = await _processorFallbackHttpClient.CheckHealthAsync(stoppingToken);
                await redisDb.StringSetAsync("processor:fallback:health", healthy);
            }
            catch (Exception)
            {
                await redisDb.StringSetAsync("processor:fallback:health", false);
            }
        }
    }
}