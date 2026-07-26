namespace SleepHunter.Interop.Memory;

public sealed record MemoryReadMetrics(
    int RequestCount,
    int TransportReadCount,
    int FailedReadCount,
    long RequestedBytes,
    long BytesRead);
