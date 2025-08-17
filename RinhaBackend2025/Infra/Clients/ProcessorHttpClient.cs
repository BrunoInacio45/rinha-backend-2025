using Polly.CircuitBreaker;
using RinhaBackend2025.Domain.Models;
using RinhaBackend2025.Models;
using RinhaBackend2025.Models.DTO;

namespace RinhaBackend2025.Infra.Clients
{
    public class ProcessorHttpClient<T> where T : class
    {
        private readonly HttpClient _http;
        private readonly AsyncCircuitBreakerPolicy<HttpResponseMessage>? _policy;

        public ProcessorHttpClient(HttpClient http, AsyncCircuitBreakerPolicy<HttpResponseMessage>? policy = null)
        {
            _http = http;
            _policy = policy;
        }

        public async Task<bool> ProcessPaymentAsync(Payment payment, CancellationToken cancellationToken = default)
        {
            var paymentRequest = new PaymentProcessorRequest(
                payment.CorrelationId,
                payment.Amount,
                payment.ProcessorAt);

            if (_policy != null)
            {
                var response = await _policy.ExecuteAsync(() =>
                    _http.PostAsJsonAsync("payments", paymentRequest, cancellationToken)
                );
                response.EnsureSuccessStatusCode();
            }
            else
            {
                var response = await _http.PostAsJsonAsync("payments", paymentRequest, cancellationToken);
                response.EnsureSuccessStatusCode();
            }

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