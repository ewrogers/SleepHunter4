namespace SleepHunter.Interop.Memory;

public readonly record struct MemoryAddress(ulong Value)
{
    public static MemoryAddress Null { get; } = new(0);

    public bool IsNull => Value == 0;

    public bool TryOffset(long offset, out MemoryAddress address)
    {
        if (offset >= 0)
        {
            var delta = (ulong)offset;
            if (Value > ulong.MaxValue - delta)
            {
                address = default;
                return false;
            }

            address = new MemoryAddress(Value + delta);
            return true;
        }

        var magnitude = offset == long.MinValue
            ? 1UL << 63
            : (ulong)-offset;
        if (Value < magnitude)
        {
            address = default;
            return false;
        }

        address = new MemoryAddress(Value - magnitude);
        return true;
    }

    public override string ToString() => $"0x{Value:X}";
}
