namespace SleepHunter.Interop.Memory;

public readonly record struct MemorySourceReadResult(
    int BytesRead,
    int NativeErrorCode = 0)
{
    public static MemorySourceReadResult Failed(int nativeErrorCode = 0) =>
        new(0, nativeErrorCode);
}
