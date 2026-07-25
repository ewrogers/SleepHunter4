namespace SleepHunter.Interop.Memory;

public sealed class MemoryReadSession
{
    private readonly IProcessMemorySource source;
    private int requestCount;
    private int transportReadCount;
    private int failedReadCount;
    private long requestedBytes;
    private long bytesRead;

    public MemoryReadSession(
        IProcessMemorySource source,
        MemoryReadLimits limits)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(limits);

        this.source = source;
        Limits = limits;
    }

    public MemoryReadLimits Limits { get; }

    public MemoryReadMetrics Metrics =>
        new(
            requestCount,
            transportReadCount,
            failedReadCount,
            requestedBytes,
            bytesRead);

    public bool TryRead(
        MemoryAddress address,
        Span<byte> destination,
        out MemoryReadError? error)
    {
        requestCount++;
        if (destination.IsEmpty)
        {
            return Fail(
                MemoryReadFailure.InvalidLength,
                address,
                requestedBytes: 0,
                out error);
        }

        if (destination.Length > Limits.MaximumBlockBytes)
        {
            return Fail(
                MemoryReadFailure.BlockLimitExceeded,
                address,
                destination.Length,
                out error);
        }

        if (!Limits.AddressRange.Contains(address, destination.Length))
        {
            return Fail(
                MemoryReadFailure.InvalidAddress,
                address,
                destination.Length,
                out error);
        }

        if (transportReadCount >= Limits.MaximumReadCount)
        {
            return Fail(
                MemoryReadFailure.ReadBudgetExceeded,
                address,
                destination.Length,
                out error);
        }

        if (requestedBytes > Limits.MaximumTotalBytes - destination.Length)
        {
            return Fail(
                MemoryReadFailure.ByteBudgetExceeded,
                address,
                destination.Length,
                out error);
        }

        transportReadCount++;
        requestedBytes += destination.Length;
        var result = source.Read(address, destination);
        if (result.BytesRead < 0 || result.BytesRead > destination.Length)
        {
            throw new InvalidOperationException(
                "The process memory source reported an invalid byte count.");
        }

        bytesRead += result.BytesRead;
        if (result.BytesRead == destination.Length)
        {
            error = null;
            return true;
        }

        var failure = result.BytesRead == 0
            ? MemoryReadFailure.TransportFailure
            : MemoryReadFailure.PartialRead;
        return Fail(
            failure,
            address,
            destination.Length,
            out error,
            result.BytesRead,
            result.NativeErrorCode);
    }

    public bool Contains(MemoryAddress address, int length = 1) =>
        Limits.AddressRange.Contains(address, length);

    private bool Fail(
        MemoryReadFailure failure,
        MemoryAddress address,
        int requestedBytes,
        out MemoryReadError? error,
        int actualBytes = 0,
        int nativeErrorCode = 0)
    {
        failedReadCount++;
        error = new MemoryReadError(
            failure,
            address,
            requestedBytes,
            actualBytes,
            nativeErrorCode);
        return false;
    }
}
