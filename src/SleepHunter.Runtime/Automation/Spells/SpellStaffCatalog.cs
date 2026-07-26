using System.Collections.Immutable;
using SleepHunter.Runtime.Automation.Staves;

namespace SleepHunter.Runtime.Automation.Spells;

public sealed class SpellStaffCatalog : IEquatable<SpellStaffCatalog>
{
    public static SpellStaffCatalog Empty { get; } = new([]);

    public SpellStaffCatalog(IEnumerable<SpellStaffCandidateSet> candidateSets)
    {
        ArgumentNullException.ThrowIfNull(candidateSets);

        var entries = candidateSets.ToImmutableArray();
        if (entries.Any(entry => entry is null))
        {
            throw new ArgumentException(
                "Spell staff catalogs cannot contain null candidate sets.",
                nameof(candidateSets));
        }

        if (entries.Select(entry => entry.EntryId).Distinct().Count() !=
            entries.Length)
        {
            throw new ArgumentException(
                "Spell staff catalogs require unique queue entry identifiers.",
                nameof(candidateSets));
        }

        CandidateSets = entries.Sort(
            static (left, right) =>
                left.EntryId.Value.CompareTo(right.EntryId.Value));
    }

    public ImmutableArray<SpellStaffCandidateSet> CandidateSets { get; }

    public ImmutableArray<StaffCandidate> GetCandidates(
        SpellQueueEntryId entryId) =>
        CandidateSets.FirstOrDefault(entry => entry.EntryId == entryId)
            ?.Candidates ?? [];

    public bool Equals(SpellStaffCatalog? other) =>
        other is not null &&
        CandidateSets.SequenceEqual(other.CandidateSets);

    public override bool Equals(object? obj) =>
        obj is SpellStaffCatalog other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var entry in CandidateSets)
        {
            hash.Add(entry);
        }

        return hash.ToHashCode();
    }
}
