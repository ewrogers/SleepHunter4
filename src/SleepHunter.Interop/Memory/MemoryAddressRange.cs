namespace SleepHunter.Interop.Memory;

public sealed record MemoryAddressRange
{
    public static MemoryAddressRange Address32Bit { get; } = new(
        new MemoryAddress(1),
        new MemoryAddress(uint.MaxValue));

    public static MemoryAddressRange Address64Bit { get; } = new(
        new MemoryAddress(1),
        new MemoryAddress(ulong.MaxValue));

    public MemoryAddressRange(
        MemoryAddress minimum,
        MemoryAddress maximum)
    {
        if (maximum.Value < minimum.Value)
        {
            throw new ArgumentException(
                "The maximum memory address cannot be below the minimum.");
        }

        Minimum = minimum;
        Maximum = maximum;
    }

    public MemoryAddress Minimum { get; }

    public MemoryAddress Maximum { get; }

    public bool Contains(MemoryAddress address, int length = 1)
    {
        if (length <= 0 ||
            address.Value < Minimum.Value ||
            address.Value > Maximum.Value)
        {
            return false;
        }

        return (ulong)(length - 1) <= Maximum.Value - address.Value;
    }
}
