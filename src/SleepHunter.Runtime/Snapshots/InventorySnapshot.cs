using System.Collections.Immutable;

namespace SleepHunter.Runtime.Snapshots;

public sealed class InventorySnapshot : IEquatable<InventorySnapshot>
{
    public static InventorySnapshot Empty { get; } = new([]);

    public InventorySnapshot(IEnumerable<InventoryItemSnapshot> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        var entries = items.ToImmutableArray();
        if (entries.Any(item => item is null))
        {
            throw new ArgumentException(
                "Inventory snapshots cannot contain null items.",
                nameof(items));
        }

        if (entries.Select(item => item.Slot).Distinct().Count() != entries.Length)
        {
            throw new ArgumentException(
                "Inventory snapshot slots must be unique.",
                nameof(items));
        }

        Items = entries.Sort(
            static (left, right) => left.Slot.CompareTo(right.Slot));
    }

    public ImmutableArray<InventoryItemSnapshot> Items { get; }

    public InventoryItemSnapshot? FindFirst(string itemName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(itemName);

        return Items.FirstOrDefault(
            item => string.Equals(
                item.Name,
                itemName.Trim(),
                StringComparison.OrdinalIgnoreCase));
    }

    public bool Equals(InventorySnapshot? other) =>
        other is not null &&
        Items.SequenceEqual(other.Items);

    public override bool Equals(object? obj) =>
        obj is InventorySnapshot other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var item in Items)
        {
            hash.Add(item);
        }

        return hash.ToHashCode();
    }
}
