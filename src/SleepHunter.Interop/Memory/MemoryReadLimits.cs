namespace SleepHunter.Interop.Memory;

public sealed record MemoryReadLimits
{
    public static MemoryReadLimits Client32Bit { get; } = new(
        PointerWidth.Bit32,
        MemoryAddressRange.Address32Bit);

    public MemoryReadLimits(
        PointerWidth pointerWidth,
        MemoryAddressRange addressRange,
        int maximumBlockBytes = 64 * 1024,
        int maximumStringBytes = 4 * 1024,
        long maximumTotalBytes = 4 * 1024 * 1024,
        int maximumReadCount = 4096,
        int maximumPointerDepth = 16)
    {
        if (!Enum.IsDefined(pointerWidth))
        {
            throw new ArgumentOutOfRangeException(
                nameof(pointerWidth),
                pointerWidth,
                "The pointer width is not supported.");
        }

        ArgumentNullException.ThrowIfNull(addressRange);

        if (maximumBlockBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumBlockBytes),
                maximumBlockBytes,
                "The maximum block length must be positive.");
        }

        if (maximumStringBytes <= 0 ||
            maximumStringBytes > maximumBlockBytes)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumStringBytes),
                maximumStringBytes,
                "The maximum string length must fit within one block.");
        }

        if (maximumTotalBytes < maximumBlockBytes)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumTotalBytes),
                maximumTotalBytes,
                "The total byte budget must fit at least one maximum block.");
        }

        if (maximumReadCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumReadCount),
                maximumReadCount,
                "The read-count budget must be positive.");
        }

        if (maximumPointerDepth <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumPointerDepth),
                maximumPointerDepth,
                "The pointer-depth limit must be positive.");
        }

        PointerWidth = pointerWidth;
        AddressRange = addressRange;
        MaximumBlockBytes = maximumBlockBytes;
        MaximumStringBytes = maximumStringBytes;
        MaximumTotalBytes = maximumTotalBytes;
        MaximumReadCount = maximumReadCount;
        MaximumPointerDepth = maximumPointerDepth;
    }

    public PointerWidth PointerWidth { get; }

    public MemoryAddressRange AddressRange { get; }

    public int MaximumBlockBytes { get; }

    public int MaximumStringBytes { get; }

    public long MaximumTotalBytes { get; }

    public int MaximumReadCount { get; }

    public int MaximumPointerDepth { get; }
}
