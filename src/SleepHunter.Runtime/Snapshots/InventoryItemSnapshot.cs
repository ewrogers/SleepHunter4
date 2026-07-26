namespace SleepHunter.Runtime.Snapshots;

public sealed record InventoryItemSnapshot
{
    public const int MaximumCollapsedSlot = 34;
    public const int MaximumUsableSlot = 59;
    public const int MaximumSlot = 60;

    public InventoryItemSnapshot(
        int slot,
        string name,
        ushort sprite = 0,
        byte dyeColor = 0,
        string? displayName = null,
        uint quantity = 1,
        bool isStackable = false,
        uint currentDurability = 0,
        uint maximumDurability = 0)
    {
        if (slot <= 0 || slot > MaximumSlot)
        {
            throw new ArgumentOutOfRangeException(
                nameof(slot),
                slot,
                $"Inventory slots must be between 1 and {MaximumSlot}.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (quantity == 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                quantity,
                "A present inventory item must have a positive quantity.");
        }

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
        DisplayName = string.IsNullOrWhiteSpace(displayName)
            ? Name
            : displayName.Trim();
        Quantity = quantity;
        IsStackable = isStackable;
        CurrentDurability = currentDurability;
        MaximumDurability = maximumDurability;
    }

    public int Slot { get; }

    public string Name { get; }

    public ushort Sprite { get; }

    public byte DyeColor { get; }

    public string DisplayName { get; }

    public uint Quantity { get; }

    public bool IsStackable { get; }

    public uint CurrentDurability { get; }

    public uint MaximumDurability { get; }
}
