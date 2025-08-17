using RinhaBackend2025.Domain.Models;
using RinhaBackend2025.Infra.Clients;
using RinhaBackend2025.Infra.Database;
using RinhaBackend2025.Infra.Queue;
using RinhaBackend2025.Models;
using StackExchange.Redis;

namespace RinhaBackend2025.Infra.Worker
{
    public class PaymentWorker : BackgroundService
    {
        private readonly PaymentQueue _queue;
        private readonly ProcessorDefaultClient _processorDefaultHttpClient;
        private readonly ProcessorFallbackClient _processorFallbackHttpClient;
        private const int _numerRetry = 3;
        private readonly PaymentRepository _repository;
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<PaymentWorker> _logger;

        public PaymentWorker(
            IServiceScopeFactory scopeFactory,
            PaymentQueue queue,
            ProcessorDefaultClient processorDefaultHttpClient,
            ProcessorFallbackClient processorFallbackHttpClient,
            IConnectionMultiplexer redis,
            ILogger<PaymentWorker> logger)
        {
            _queue = queue;
            _processorDefaultHttpClient = processorDefaultHttpClient;
            _processorFallbackHttpClient = processorFallbackHttpClient;
            _redis = redis;
            _repository = scopeFactory.CreateScope().ServiceProvider.GetRequiredService<PaymentRepository>();
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var redisDb = _redis.GetDatabase();
            await foreach (var payment in _queue.ConsumeAsync(stoppingToken))
            {
                try
                {
                    if (payment.NumberTry > _numerRetry) continue;
                    _logger.LogInformation("Processando pagamento {Id} de {Valor}", payment.CorrelationId, payment.Amount);

                    payment.IncrementTry();

                    var defaultRedisValue = await redisDb.StringGetAsync("processor:default:health");
                    var fallbackRedisValue = await redisDb.StringGetAsync("processor:fallback:health");

                    if (IsHealthy(defaultRedisValue))
                    {
                        try
                        {
                            await RunProcessorDefault(payment, stoppingToken);
                        }
                        catch (System.Exception)
                        {
                            await RunProcessorFallback(payment, stoppingToken);
                        }

                    }
                    else if (IsHealthy(fallbackRedisValue))
                    {
                        try
                        {
                            await RunProcessorFallback(payment, stoppingToken);
                        }
                        catch (System.Exception)
                        {
                            throw;
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Nenhum processador de pagamento está saudável. Reenviando pagamento {Id} para a fila", payment.CorrelationId);
                        await _queue.PublishAsync(payment);
                        continue;
                    }
                    
                    await SavePayment(payment, stoppingToken);
                    _logger.LogInformation("Pagamento {Id} processado com sucesso", payment.CorrelationId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar pagamento {Id}", payment.CorrelationId);
                    await _queue.PublishAsync(payment);
                }
            }
        }

        private async Task RunProcessorDefault(Payment payment, CancellationToken stoppingToken)
        {
            await _processorDefaultHttpClient.ProcessPaymentAsync(payment, stoppingToken);
            payment.SetProcessor(PaymentProcessorEnum.Default);
        }

        private async Task RunProcessorFallback(Payment payment, CancellationToken stoppingToken)
        {
            await _processorFallbackHttpClient.ProcessPaymentAsync(payment, stoppingToken);
            payment.SetProcessor(PaymentProcessorEnum.Fallback);
        }

        private async Task SavePayment(Payment payment, CancellationToken stoppingToken)
        {
            await _repository.AddAsync(payment, stoppingToken);
        }

        private bool IsHealthy(RedisValue value)
        {
            return value.HasValue && (value == "1");
        }
    }
}