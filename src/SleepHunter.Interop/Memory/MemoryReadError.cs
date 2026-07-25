namespace SleepHunter.Interop.Memory;

public sealed record MemoryReadError(
    MemoryReadFailure Failure,
    MemoryAddress Address,
    int RequestedBytes,
    int BytesRead = 0,
    int NativeErrorCode = 0);
