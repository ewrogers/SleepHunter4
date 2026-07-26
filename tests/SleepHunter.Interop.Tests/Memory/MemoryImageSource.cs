using System.Buffers.Binary;
using SleepHunter.Interop.Memory;

namespace SleepHunter.Interop.Tests.Memory;

internal sealed class MemoryImageSource : IProcessMemorySource
{
    private readonly Dictionary<ulong, byte> memory = [];

    public List<(MemoryAddress Address, int Length)> Reads { get; } = [];

    public Action<MemoryAddress, int>? ReadStarting { get; set; }

    public MemorySourceReadResult Read(
        MemoryAddress address,
        Span<byte> destination)
    {
        Reads.Add((address, destination.Length));
        ReadStarting?.Invoke(address, destination.Length);

        var bytesRead = 0;
        for (var index = 0; index < destination.Length; index++)
        {
            if (!memory.TryGetValue(address.Value + (ulong)index, out var value))
            {
                break;
            }

            destination[index] = value;
            bytesRead++;
        }

        return new MemorySourceReadResult(
            bytesRead,
            bytesRead == destination.Length ? 0 : 299);
    }

    public void Write(MemoryAddress address, params byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);

        for (var index = 0; index < bytes.Length; index++)
        {
            memory[address.Value + (ulong)index] = bytes[index];
        }
    }

    public void WriteUInt32(MemoryAddress address, uint value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(uint)];
        BinaryPrimitives.WriteUInt32LittleEndian(buffer, value);
        Write(address, buffer.ToArray());
    }

    public void WriteInt32(MemoryAddress address, int value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(buffer, value);
        Write(address, buffer.ToArray());
    }

    public void WriteUInt64(MemoryAddress address, ulong value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(ulong)];
        BinaryPrimitives.WriteUInt64LittleEndian(buffer, value);
        Write(address, buffer.ToArray());
    }

    public void Clear(MemoryAddress address, int length)
    {
        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(length),
                length,
                "The cleared memory length cannot be negative.");
        }

        for (var index = 0; index < length; index++)
        {
            memory.Remove(address.Value + (ulong)index);
        }
    }
}
