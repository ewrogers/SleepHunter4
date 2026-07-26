using System.Collections.Immutable;

namespace SleepHunter.Runtime.Snapshots;

public sealed class GroupSnapshot : IEquatable<GroupSnapshot>
{
    public static GroupSnapshot Empty { get; } = new([]);

    public GroupSnapshot(IEnumerable<GroupMemberSnapshot> members)
    {
        ArgumentNullException.ThrowIfNull(members);

        var entries = members.ToImmutableArray();
        if (entries.Any(member => member is null))
        {
            throw new ArgumentException(
                "Group snapshots cannot contain null members.",
                nameof(members));
        }

        if (entries
            .Select(member => member.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count() != entries.Length)
        {
            throw new ArgumentException(
                "Group member names must be unique.",
                nameof(members));
        }

        Members = entries;
    }

    public ImmutableArray<GroupMemberSnapshot> Members { get; }

    public bool IsGrouped => !Members.IsEmpty;

    public bool Equals(GroupSnapshot? other) =>
        other is not null &&
        Members.SequenceEqual(other.Members);

    public override bool Equals(object? obj) =>
        obj is GroupSnapshot other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var member in Members)
        {
            hash.Add(member);
        }

        return hash.ToHashCode();
    }
}
