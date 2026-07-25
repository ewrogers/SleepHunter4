using SleepHunter.Interop.Memory;

namespace SleepHunter.Interop.Mappings;

public sealed record MemoryVariableDefinition
{
    public MemoryVariableDefinition(
        string key,
        PointerChain address,
        MemoryValueKind valueKind = MemoryValueKind.Text,
        int maximumLength = 0,
        int recordSize = 0,
        int capacity = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(address);

        if (!Enum.IsDefined(valueKind))
        {
            throw new ArgumentOutOfRangeException(
                nameof(valueKind),
                valueKind,
                "The memory value kind is not supported.");
        }

        if (maximumLength < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumLength),
                maximumLength,
                "The maximum value length cannot be negative.");
        }

        if (recordSize < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(recordSize),
                recordSize,
                "The record size cannot be negative.");
        }

        if (capacity < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(capacity),
                capacity,
                "The collection capacity cannot be negative.");
        }

        if (valueKind == MemoryValueKind.Text && maximumLength <= 0)
        {
            throw new ArgumentException(
                "String memory variables require a positive maximum length.",
                nameof(maximumLength));
        }

        Key = key.Trim();
        Address = address;
        ValueKind = valueKind;
        MaximumLength = maximumLength;
        RecordSize = recordSize;
        Capacity = capacity;
    }

    public string Key { get; }

    public PointerChain Address { get; }

    public MemoryValueKind ValueKind { get; }

    public int MaximumLength { get; }

    public int RecordSize { get; }

    public int Capacity { get; }
}
