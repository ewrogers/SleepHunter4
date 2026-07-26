using System.Collections.Immutable;

namespace SleepHunter.Runtime.Snapshots;

public sealed class SpellbookSnapshot : IEquatable<SpellbookSnapshot>
{
    public static SpellbookSnapshot Empty { get; } = new([]);

    public SpellbookSnapshot(IEnumerable<SpellSnapshot> spells)
    {
        ArgumentNullException.ThrowIfNull(spells);

        var entries = spells.ToImmutableArray();
        if (entries.Any(spell => spell is null))
        {
            throw new ArgumentException(
                "Spellbook snapshots cannot contain null spells.",
                nameof(spells));
        }

        if (entries.Select(spell => spell.Slot).Distinct().Count() != entries.Length)
        {
            throw new ArgumentException(
                "Spellbook snapshot slots must be unique.",
                nameof(spells));
        }

        if (entries
            .Select(spell => spell.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count() != entries.Length)
        {
            throw new ArgumentException(
                "Spellbook snapshot names must be unique.",
                nameof(spells));
        }

        Spells = entries.Sort(
            static (left, right) => left.Slot.CompareTo(right.Slot));
    }

    public ImmutableArray<SpellSnapshot> Spells { get; }

    public SpellSnapshot? Find(string spellName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(spellName);

        return Spells.FirstOrDefault(
            spell => string.Equals(
                spell.Name,
                spellName.Trim(),
                StringComparison.OrdinalIgnoreCase));
    }

    public bool Equals(SpellbookSnapshot? other) =>
        other is not null &&
        Spells.SequenceEqual(other.Spells);

    public override bool Equals(object? obj) =>
        obj is SpellbookSnapshot other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var spell in Spells)
        {
            hash.Add(spell);
        }

        return hash.ToHashCode();
    }
}
