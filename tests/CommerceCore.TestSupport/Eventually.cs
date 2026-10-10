namespace CommerceCore.TestSupport;

public static class Eventually
{
    // Polls until the condition holds or the timeout passes. Asynchronous flows need this
    // instead of fixed sleeps.
    public static async Task<T> ReadAsync<T>(
        Func<Task<T>> read, Func<T, bool> isDone, TimeSpan timeout, string description)
    {
        var deadline = DateTime.UtcNow + timeout;
        var value = await read();

        while (!isDone(value))
        {
            if (DateTime.UtcNow > deadline)
                throw new TimeoutException($"Timed out after {timeout.TotalSeconds:0}s waiting for: {description}. Last value: {value}");

            await Task.Delay(250);
            value = await read();
        }

        return value;
    }
}
