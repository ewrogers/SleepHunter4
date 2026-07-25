using System.Threading.Channels;

namespace SleepHunter.Runtime.Hosting;

internal sealed class LatestValueMailbox<T>
    where T : class
{
    private readonly Channel<T> channel = Channel.CreateBounded<T>(
        new BoundedChannelOptions(1)
        {
            AllowSynchronousContinuations = false,
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });

    public bool TryWrite(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return channel.Writer.TryWrite(value);
    }

    public bool TryReadLatest(out T value)
    {
        if (!channel.Reader.TryRead(out value!))
        {
            value = default!;
            return false;
        }

        while (channel.Reader.TryRead(out var newerValue))
        {
            value = newerValue;
        }

        return true;
    }

    public void Complete() => channel.Writer.TryComplete();
}
