using System.Collections.Immutable;

namespace SleepHunter.Runtime.Snapshots;

public sealed class ActiveSpellEffectsSnapshot :
    IEquatable<ActiveSpellEffectsSnapshot>
{
    public static ActiveSpellEffectsSnapshot Empty { get; } = new([]);

    public ActiveSpellEffectsSnapshot(
        IEnumerable<ActiveSpellEffectSnapshot> effects)
    {
        ArgumentNullException.ThrowIfNull(effects);

        var entries = effects.ToImmutableArray();
        if (entries.Any(effect => effect is null))
        {
            throw new ArgumentException(
                "Active spell effects cannot contain null entries.",
                nameof(effects));
        }

        if (entries.Select(effect => effect.Slot).Distinct().Count() !=
            entries.Length)
        {
            throw new ArgumentException(
                "Active spell effect slots must be unique.",
                nameof(effects));
        }

        Effects = entries.Sort(
            static (left, right) => left.Slot.CompareTo(right.Slot));
    }

    public ImmutableArray<ActiveSpellEffectSnapshot> Effects { get; }

    public bool Equals(ActiveSpellEffectsSnapshot? other) =>
        other is not null &&
        Effects.SequenceEqual(other.Effects);

    public override bool Equals(object? obj) =>
        obj is ActiveSpellEffectsSnapshot other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var effect in Effects)
        {
            hash.Add(effect);
        }

        return hash.ToHashCode();
    }
}
