using System.Collections.Immutable;

namespace SleepHunter.Runtime.Snapshots;

public sealed class EquipmentSnapshot : IEquatable<EquipmentSnapshot>
{
    public const int WeaponSlot = 1;
    public const int ShieldSlot = 3;

    public static EquipmentSnapshot Empty { get; } = new([]);

    public EquipmentSnapshot(
        string? weaponName,
        string? shieldName = null)
        : this(CreateItems(weaponName, shieldName))
    {
    }

    public EquipmentSnapshot(IEnumerable<EquipmentItemSnapshot> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        var entries = items.ToImmutableArray();
        if (entries.Any(item => item is null))
        {
            throw new ArgumentException(
                "Equipment snapshots cannot contain null items.",
                nameof(items));
        }

        if (entries.Select(item => item.Slot).Distinct().Count() !=
            entries.Length)
        {
            throw new ArgumentException(
                "Equipment snapshot slots must be unique.",
                nameof(items));
        }

        Items = entries.Sort(
            static (left, right) => left.Slot.CompareTo(right.Slot));
    }

    public ImmutableArray<EquipmentItemSnapshot> Items { get; }

    public string? WeaponName => Find(WeaponSlot)?.Name;

    public string? ShieldName => Find(ShieldSlot)?.Name;

    public bool IsDisarmed => WeaponName is null && ShieldName is null;

    public EquipmentItemSnapshot? Find(int slot) =>
        Items.FirstOrDefault(item => item.Slot == slot);

    public bool Equals(EquipmentSnapshot? other) =>
        other is not null &&
        Items.SequenceEqual(other.Items);

    public override bool Equals(object? obj) =>
        obj is EquipmentSnapshot other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var item in Items)
        {
            hash.Add(item);
        }

        return hash.ToHashCode();
    }

    private static IEnumerable<EquipmentItemSnapshot> CreateItems(
        string? weaponName,
        string? shieldName)
    {
        if (!string.IsNullOrWhiteSpace(weaponName))
        {
            yield return new EquipmentItemSnapshot(
                WeaponSlot,
                weaponName);
        }

        if (!string.IsNullOrWhiteSpace(shieldName))
        {
            yield return new EquipmentItemSnapshot(
                ShieldSlot,
                shieldName);
        }
    }
}
