using System.Collections.Immutable;
using SleepHunter.Runtime.Automation.Staves;

namespace SleepHunter.Runtime.Automation.Flowering;

public sealed record FlowerStaffCandidateSet
{
    public FlowerStaffCandidateSet(
        FlowerActionKind action,
        IEnumerable<StaffCandidate> candidates)
    {
        if (!Enum.IsDefined(action))
        {
            throw new ArgumentOutOfRangeException(
                nameof(action),
                action,
                "The flower action is not supported.");
        }

        ArgumentNullException.ThrowIfNull(candidates);

        var entries = candidates.ToImmutableArray();
        if (entries.Any(candidate => candidate is null))
        {
            throw new ArgumentException(
                "Flower staff candidate sets cannot contain null values.",
                nameof(candidates));
        }

        if (entries
            .Select(candidate => candidate.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count() != entries.Length)
        {
            throw new ArgumentException(
                "Flower staff candidates must have unique names.",
                nameof(candidates));
        }

        Action = action;
        Candidates = entries;
    }

    public FlowerActionKind Action { get; }

    public ImmutableArray<StaffCandidate> Candidates { get; }
}
