using System.Collections.Immutable;
using SleepHunter.Runtime.Automation.Staves;

namespace SleepHunter.Runtime.Automation.Spells;

public sealed class SpellStaffCandidateSet : IEquatable<SpellStaffCandidateSet>
{
    public SpellStaffCandidateSet(
        SpellQueueEntryId entryId,
        IEnumerable<StaffCandidate> candidates)
    {
        if (entryId.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(entryId),
                entryId,
                "Spell staff candidates require a valid queue entry identifier.");
        }

        ArgumentNullException.ThrowIfNull(candidates);

        var entries = candidates.ToImmutableArray();
        if (entries.Any(candidate => candidate is null))
        {
            throw new ArgumentException(
                "Spell staff candidates cannot contain null values.",
                nameof(candidates));
        }

        if (entries
            .Select(candidate => candidate.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count() != entries.Length)
        {
            throw new ArgumentException(
                "Spell staff candidate names must be unique for each queue entry.",
                nameof(candidates));
        }

        EntryId = entryId;
        Candidates = entries;
    }

    public SpellQueueEntryId EntryId { get; }

    public ImmutableArray<StaffCandidate> Candidates { get; }

    public bool Equals(SpellStaffCandidateSet? other) =>
        other is not null &&
        EntryId == other.EntryId &&
        Candidates.SequenceEqual(other.Candidates);

    public override bool Equals(object? obj) =>
        obj is SpellStaffCandidateSet other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(EntryId);
        foreach (var candidate in Candidates)
        {
            hash.Add(candidate);
        }

        return hash.ToHashCode();
    }
}
