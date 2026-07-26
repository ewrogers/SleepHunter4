using System.Threading.Channels;

namespace SleepHunter.Runtime.Tests.Hosting;

internal static class ChannelReaderTestExtensions
{
    public static async Task<T> ReadUntilAsync<T>(
        this ChannelReader<T> reader,
        Func<T, bool> predicate)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        try
        {
            await foreach (var value in reader.ReadAllAsync(timeout.Token))
            {
                if (predicate(value))
                {
                    return value;
                }
            }
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            throw new TimeoutException("The expected channel value was not published.");
        }

        throw new InvalidOperationException(
            "The channel completed before the expected value was published.");
    }
}
