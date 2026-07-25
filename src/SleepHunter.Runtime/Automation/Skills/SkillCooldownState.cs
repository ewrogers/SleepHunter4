using System.Collections.Immutable;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Automation.Skills;

public sealed class SkillCooldownState : IEquatable<SkillCooldownState>
{
    private static readonly StringComparer NameComparer =
        StringComparer.OrdinalIgnoreCase;

    public static SkillCooldownState Empty { get; } = new(
        ImmutableDictionary.Create<string, MacroTimestamp>(NameComparer));

    private SkillCooldownState(
        ImmutableDictionary<string, MacroTimestamp> readyAtBySkill)
    {
        ReadyAtBySkill = readyAtBySkill;
    }

    public ImmutableDictionary<string, MacroTimestamp> ReadyAtBySkill { get; }

    public SkillCooldownState WithCooldown(
        string skillName,
        MacroTimestamp readyAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(skillName);

        var entries = ReadyAtBySkill.SetItem(skillName.Trim(), readyAt);
        return ReferenceEquals(entries, ReadyAtBySkill)
            ? this
            : new SkillCooldownState(entries);
    }

    public SkillCooldownState Clear(string skillName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(skillName);

        var entries = ReadyAtBySkill.Remove(skillName.Trim());
        return ReferenceEquals(entries, ReadyAtBySkill)
            ? this
            : new SkillCooldownState(entries);
    }

    public SkillCooldownState Prune(MacroTimestamp currentTime)
    {
        var entries = ReadyAtBySkill.RemoveRange(
            ReadyAtBySkill
                .Where(entry => entry.Value <= currentTime)
                .Select(entry => entry.Key));

        return ReferenceEquals(entries, ReadyAtBySkill)
            ? this
            : new SkillCooldownState(entries);
    }

    public MacroTimestamp? GetReadyAt(
        string skillName,
        MacroTimestamp currentTime)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(skillName);

        return ReadyAtBySkill.TryGetValue(skillName.Trim(), out var readyAt) &&
               readyAt > currentTime
            ? readyAt
            : null;
    }

    public bool Equals(SkillCooldownState? other) =>
        other is not null &&
        ReadyAtBySkill.Count == other.ReadyAtBySkill.Count &&
        ReadyAtBySkill.All(
            entry =>
                other.ReadyAtBySkill.TryGetValue(entry.Key, out var readyAt) &&
                readyAt == entry.Value);

    public override bool Equals(object? obj) =>
        obj is SkillCooldownState other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var entry in ReadyAtBySkill.OrderBy(
                     entry => entry.Key,
                     NameComparer))
        {
            hash.Add(entry.Key, NameComparer);
            hash.Add(entry.Value);
        }

        return hash.ToHashCode();
    }
}
