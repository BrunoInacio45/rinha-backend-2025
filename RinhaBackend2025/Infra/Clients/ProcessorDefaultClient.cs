using Polly;

namespace RinhaBackend2025.Infra.Clients
{
    public class ProcessorDefaultClient : ProcessorHttpClient<ProcessorDefaultClient>
    {
        public ProcessorDefaultClient(HttpClient http)
            : base(http, Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .OrResult(r => !r.IsSuccessStatusCode)
                .CircuitBreakerAsync(3, TimeSpan.FromSeconds(5)))
        {
        }
    }
}