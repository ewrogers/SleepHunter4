namespace SleepHunter.Runtime.Snapshots;

public sealed record InventoryItemSnapshot
{
    public const int MaximumCollapsedSlot = 34;
    public const int MaximumUsableSlot = 59;
    public const int MaximumSlot = 60;

    public InventoryItemSnapshot(int slot, string name)
    {
        if (slot <= 0 || slot > MaximumSlot)
        {
            throw new ArgumentOutOfRangeException(
                nameof(slot),
                slot,
                $"Inventory slots must be between 1 and {MaximumSlot}.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Slot = slot;
        Name = name.Trim();
    }

    public int Slot { get; }

    public string Name { get; }
}
