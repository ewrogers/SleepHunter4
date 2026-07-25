using System.Collections.Immutable;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Automation.Spells;

public sealed class SpellCooldownState : IEquatable<SpellCooldownState>
{
    private static readonly StringComparer NameComparer =
        StringComparer.OrdinalIgnoreCase;

    public static SpellCooldownState Empty { get; } = new(
        ImmutableDictionary.Create<string, MacroTimestamp>(NameComparer));

    private SpellCooldownState(
        ImmutableDictionary<string, MacroTimestamp> readyAtBySpell)
    {
        ReadyAtBySpell = readyAtBySpell;
    }

    public ImmutableDictionary<string, MacroTimestamp> ReadyAtBySpell { get; }

    public SpellCooldownState WithCooldown(
        string spellName,
        MacroTimestamp readyAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(spellName);

        var entries = ReadyAtBySpell.SetItem(spellName.Trim(), readyAt);
        return ReferenceEquals(entries, ReadyAtBySpell)
            ? this
            : new SpellCooldownState(entries);
    }

    public SpellCooldownState Clear(string spellName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(spellName);

        var entries = ReadyAtBySpell.Remove(spellName.Trim());
        return ReferenceEquals(entries, ReadyAtBySpell)
            ? this
            : new SpellCooldownState(entries);
    }

    public SpellCooldownState Prune(MacroTimestamp currentTime)
    {
        var entries = ReadyAtBySpell.RemoveRange(
            ReadyAtBySpell
                .Where(entry => entry.Value <= currentTime)
                .Select(entry => entry.Key));

        return ReferenceEquals(entries, ReadyAtBySpell)
            ? this
            : new SpellCooldownState(entries);
    }

    public MacroTimestamp? GetReadyAt(
        string spellName,
        MacroTimestamp currentTime)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(spellName);

        return ReadyAtBySpell.TryGetValue(spellName.Trim(), out var readyAt) &&
               readyAt > currentTime
            ? readyAt
            : null;
    }

    public bool Equals(SpellCooldownState? other) =>
        other is not null &&
        ReadyAtBySpell.Count == other.ReadyAtBySpell.Count &&
        ReadyAtBySpell.All(
            entry =>
                other.ReadyAtBySpell.TryGetValue(entry.Key, out var readyAt) &&
                readyAt == entry.Value);

    public override bool Equals(object? obj) =>
        obj is SpellCooldownState other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var entry in ReadyAtBySpell.OrderBy(
                     entry => entry.Key,
                     NameComparer))
        {
            hash.Add(entry.Key, NameComparer);
            hash.Add(entry.Value);
        }

        return hash.ToHashCode();
    }
}
