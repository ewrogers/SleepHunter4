namespace SleepHunter.Interop.Memory;

public interface IProcessMemorySource
{
    MemorySourceReadResult Read(
        MemoryAddress address,
        Span<byte> destination);
}
