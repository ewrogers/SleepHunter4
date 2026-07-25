using System.Collections.Immutable;

namespace SleepHunter.Runtime.Snapshots;

public sealed class SkillbookSnapshot : IEquatable<SkillbookSnapshot>
{
    public static SkillbookSnapshot Empty { get; } = new([]);

    public SkillbookSnapshot(IEnumerable<SkillSnapshot> skills)
    {
        ArgumentNullException.ThrowIfNull(skills);

        var entries = skills.ToImmutableArray();
        if (entries.Any(skill => skill is null))
        {
            throw new ArgumentException(
                "Skillbook snapshots cannot contain null skills.",
                nameof(skills));
        }

        if (entries.Select(skill => skill.Slot).Distinct().Count() !=
            entries.Length)
        {
            throw new ArgumentException(
                "Skillbook snapshot slots must be unique.",
                nameof(skills));
        }

        if (entries
            .Select(skill => skill.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count() != entries.Length)
        {
            throw new ArgumentException(
                "Skillbook snapshot names must be unique.",
                nameof(skills));
        }

        Skills = entries.Sort(
            static (left, right) => left.Slot.CompareTo(right.Slot));
    }

    public ImmutableArray<SkillSnapshot> Skills { get; }

    public SkillSnapshot? Find(string skillName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(skillName);

        return Skills.FirstOrDefault(
            skill => string.Equals(
                skill.Name,
                skillName.Trim(),
                StringComparison.OrdinalIgnoreCase));
    }

    public bool Equals(SkillbookSnapshot? other) =>
        other is not null &&
        Skills.SequenceEqual(other.Skills);

    public override bool Equals(object? obj) =>
        obj is SkillbookSnapshot other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var skill in Skills)
        {
            hash.Add(skill);
        }

        return hash.ToHashCode();
    }
}
