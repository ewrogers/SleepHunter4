using System.Collections.Immutable;
using SleepHunter.Runtime.Automation.Staves;

namespace SleepHunter.Runtime.Automation.Flowering;

public sealed class FlowerStaffCatalog : IEquatable<FlowerStaffCatalog>
{
    public static FlowerStaffCatalog Empty { get; } = new([]);

    public FlowerStaffCatalog(
        IEnumerable<FlowerStaffCandidateSet> candidateSets)
    {
        ArgumentNullException.ThrowIfNull(candidateSets);

        var entries = candidateSets.ToImmutableArray();
        if (entries.Any(entry => entry is null))
        {
            throw new ArgumentException(
                "Flower staff catalogs cannot contain null candidate sets.",
                nameof(candidateSets));
        }

        if (entries.Select(entry => entry.Action).Distinct().Count() !=
            entries.Length)
        {
            throw new ArgumentException(
                "Flower staff catalogs require unique actions.",
                nameof(candidateSets));
        }

        CandidateSets = entries.Sort(
            static (left, right) =>
                left.Action.CompareTo(right.Action));
    }

    public ImmutableArray<FlowerStaffCandidateSet> CandidateSets { get; }

    public ImmutableArray<StaffCandidate> GetCandidates(
        FlowerActionKind action) =>
        CandidateSets.FirstOrDefault(entry => entry.Action == action)
            ?.Candidates ?? [];

    public bool Equals(FlowerStaffCatalog? other) =>
        other is not null &&
        CandidateSets.SequenceEqual(other.CandidateSets);

    public override bool Equals(object? obj) =>
        obj is FlowerStaffCatalog other && Equals(other);

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
