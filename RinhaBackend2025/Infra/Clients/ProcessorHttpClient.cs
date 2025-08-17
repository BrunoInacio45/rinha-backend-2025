using System.Text;
using System.Text.Json;
using RinhaBackend2025.Domain.Models;
using RinhaBackend2025.Models;
using RinhaBackend2025.Models.DTO;

namespace RinhaBackend2025.Infra.Clients
{
    public class ProcessorHttpClient<T> where T : class
    {
        private readonly HttpClient _http;

        public ProcessorHttpClient(HttpClient http)
        {
            _http = http;
        }

        public async Task<bool> ProcessPaymentAsync(Payment payment, CancellationToken cancellationToken = default)
        {
            var paymentRequest = new PaymentProcessorRequest(
                payment.CorrelationId,
                payment.Amount,
                payment.ProcessorAt);

            var response = await _http.PostAsJsonAsync("payments", paymentRequest, cancellationToken);
            response.EnsureSuccessStatusCode();

            return true;
        }

        public async Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            var response = await _http.GetAsync("payments/service-health", cancellationToken);
            response.EnsureSuccessStatusCode();

            var health = await response.Content.ReadFromJsonAsync<HealthResponse>(cancellationToken: cancellationToken);

            if (health is null || health.Failing != false)
                return false;

            return true;
        }
    }
}