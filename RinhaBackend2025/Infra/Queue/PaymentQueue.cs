using System.Threading.Channels;
using RinhaBackend2025.Domain.Models;

namespace RinhaBackend2025.Infra.Queue
{
    public class PaymentQueue
    {
        private readonly Channel<Payment> _channel = Channel.CreateUnbounded<Payment>();

        public async Task PublishAsync(Payment payment)
        {
            await _channel.Writer.WriteAsync(payment);
        }

        public IAsyncEnumerable<Payment> ConsumeAsync(CancellationToken ct = default)
        {
            return _channel.Reader.ReadAllAsync(ct);
        }
    }
}