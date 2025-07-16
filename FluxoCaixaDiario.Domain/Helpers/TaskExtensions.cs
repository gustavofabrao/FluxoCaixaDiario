namespace FluxoCaixaDiario.Domain.Helpers
{
    public static class TaskExtensions
    {
        public static async Task<T> WithTimeout<T>(this Task<T> task, TimeSpan timeout)
        {
            if (await Task.WhenAny(task, Task.Delay(timeout)) != task)
            {
                throw new TimeoutException($"Operação sofreu time out depois de {timeout.TotalSeconds} segundos");
            }
            return await task;
        }
    }
}
