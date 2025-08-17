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
        private readonly RedisPaymentQueue _queue;
        private readonly ProcessorDefaultClient _processorDefault;
        private readonly ProcessorFallbackClient _processorFallback;
        private readonly PaymentRepository _repository;
        private readonly IConnectionMultiplexer _redis;

        private const int MaxDegreeOfParallelism = 4;
        private const int BatchSize = 5;
        private const int MaxRetries = 3;

        public PaymentWorker(
            IServiceScopeFactory scopeFactory,
            RedisPaymentQueue queue,
            ProcessorDefaultClient processorDefault,
            ProcessorFallbackClient processorFallback,
            IConnectionMultiplexer redis)
        {
            _queue = queue;
            _processorDefault = processorDefault;
            _processorFallback = processorFallback;
            _repository = scopeFactory.CreateScope().ServiceProvider.GetRequiredService<PaymentRepository>();
            _redis = redis;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var tasks = new List<Task>();
            var redisDb = _redis.GetDatabase();

            while (!stoppingToken.IsCancellationRequested)
            {
                var batch = await _queue.ConsumeBatchAsync(BatchSize, stoppingToken);
                if (batch.Count == 0)
                {
                    await Task.Delay(50, stoppingToken);
                    continue;
                }

                foreach (var payment in batch)
                {
                    tasks.Add(ProcessPaymentAsync(payment, redisDb, stoppingToken));

                    if (tasks.Count >= MaxDegreeOfParallelism)
                    {
                        var completed = await Task.WhenAny(tasks);
                        tasks.RemoveAll(t => t.IsCompleted);
                    }
                }
            }

            await Task.WhenAll(tasks);
        }

        private async Task ProcessPaymentAsync(Payment payment, IDatabase redisDb, CancellationToken ct)
        {
            try
            {
                await RetryPolicy.ExecuteWithRetryAsync(async () =>
                {
                    payment.IncrementTry();

                    var defaultHealth = await redisDb.StringGetAsync("processor:default:health");
                    var fallbackHealth = await redisDb.StringGetAsync("processor:fallback:health");

                    if (IsHealthy(defaultHealth))
                    {
                        try
                        {
                            await _processorDefault.ProcessPaymentAsync(payment, ct);
                            payment.SetProcessor(PaymentProcessorEnum.Default);
                        }
                        catch
                        {
                            await TryFallback(payment, fallbackHealth, ct);
                        }
                    }
                    else
                    {
                        await TryFallback(payment, fallbackHealth, ct);
                    }

                    await _repository.AddAsync(payment, ct);
                });
            }
            catch (Exception)
            {
                await _queue.PublishAsync(payment); // mantém o pagamento seguro na fila
            }
        }

        private async Task TryFallback(Payment payment, RedisValue fallbackHealth, CancellationToken ct)
        {
            if (IsHealthy(fallbackHealth))
            {
                try
                {
                    await _processorFallback.ProcessPaymentAsync(payment, ct);
                    payment.SetProcessor(PaymentProcessorEnum.Fallback);
                }
                catch
                {
                    throw; 
                }
            }
            else
            {
                throw new Exception("Nenhum processador disponível");
            }
        }

        private bool IsHealthy(RedisValue value) => value.HasValue && value == "1";
    }

}