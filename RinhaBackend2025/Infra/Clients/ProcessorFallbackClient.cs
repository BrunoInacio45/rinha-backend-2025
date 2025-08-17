namespace RinhaBackend2025.Infra.Clients
{
    public class ProcessorFallbackClient : ProcessorHttpClient<ProcessorFallbackClient>
    {
        public ProcessorFallbackClient(HttpClient http) : base(http)
        {
        }
    }
}