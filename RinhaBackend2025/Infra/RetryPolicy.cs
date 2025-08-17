namespace RinhaBackend2025.Infra
{
    public static class RetryPolicy
    {
        private const int MaxRetries = 3;
        private static readonly TimeSpan[] BackoffDelays =
            { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4) };

        public static async Task ExecuteWithRetryAsync(Func<Task> action)
        {
            for (int i = 0; i < MaxRetries; i++)
            {
                try
                {
                    await action();
                    return;
                }
                catch
                {
                    if (i == MaxRetries - 1) throw;
                    await Task.Delay(BackoffDelays[i]);
                }
            }
        }
    }

}