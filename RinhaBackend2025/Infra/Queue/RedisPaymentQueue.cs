using RinhaBackend2025.Domain.Models;

namespace RinhaBackend2025.Infra.Queue
{
    using StackExchange.Redis;
    using System.Text.Json;

    public class RedisPaymentQueue
    {
        private readonly IDatabase _db;
        private const string StreamKey = "payments";

        public RedisPaymentQueue(IConnectionMultiplexer redis)
        {
            _db = redis.GetDatabase();
        }

        public async Task PublishAsync(Payment payment)
        {
            var json = JsonSerializer.Serialize(payment);
            await _db.ListLeftPushAsync(StreamKey, json);
        }

        public async Task<List<Payment>> ConsumeBatchAsync(int batchSize, CancellationToken ct)
        {
            var list = new List<Payment>();
            for (int i = 0; i < batchSize; i++)
            {
                ct.ThrowIfCancellationRequested();
                var value = await _db.ListRightPopAsync(StreamKey);
                if (!value.HasValue) break;
                var payment = JsonSerializer.Deserialize<Payment>(value!);
                if (payment != null) list.Add(payment);
            }
            return list;
        }
    }
}