namespace RinhaBackend2025.Infra.Clients
{
    public class ProcessorDefaultClient : ProcessorHttpClient<ProcessorDefaultClient>
    {
        public ProcessorDefaultClient(HttpClient http) : base(http)
        {
        }
    }
}