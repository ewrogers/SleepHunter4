namespace SleepHunter.Runtime.Snapshots;

public sealed record EquipmentItemSnapshot
{
    public const int MaximumSlot = 18;

    public EquipmentItemSnapshot(
        int slot,
        string name,
        ushort sprite = 0,
        byte dyeColor = 0,
        uint currentDurability = 0,
        uint maximumDurability = 0)
    {
        if (slot is <= 0 or > MaximumSlot)
        {
            throw new ArgumentOutOfRangeException(
                nameof(slot),
                slot,
                $"Equipment slots must be between 1 and {MaximumSlot}.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (maximumDurability > 0 &&
            currentDurability > maximumDurability)
        {
            throw new ArgumentOutOfRangeException(
                nameof(currentDurability),
                currentDurability,
                "Current durability cannot exceed maximum durability.");
        }

        Slot = slot;
        Name = name.Trim();
        Sprite = sprite;
        DyeColor = dyeColor;
        CurrentDurability = currentDurability;
        MaximumDurability = maximumDurability;
    }

    public int Slot { get; }

    public string Name { get; }

    public ushort Sprite { get; }

    public byte DyeColor { get; }

    public uint CurrentDurability { get; }

    public uint MaximumDurability { get; }
}
