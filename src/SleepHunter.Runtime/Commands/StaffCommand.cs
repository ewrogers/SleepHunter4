using System.Collections.Immutable;
using SleepHunter.Runtime.Automation.Staves;

namespace SleepHunter.Runtime.Commands;

public sealed record RequestStaffSwitchCommand : MacroCommand
{
    public RequestStaffSwitchCommand(
        int baseCastLines,
        IEnumerable<StaffCandidate> candidates,
        StaffEquipmentPolicy? policy = null)
    {
        if (baseCastLines < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(baseCastLines),
                baseCastLines,
                "Base cast lines cannot be negative.");
        }

        ArgumentNullException.ThrowIfNull(candidates);

        var candidateEntries = candidates.ToImmutableArray();
        if (candidateEntries.Any(candidate => candidate is null))
        {
            throw new ArgumentException(
                "Staff switch candidates cannot contain null values.",
                nameof(candidates));
        }

        if (candidateEntries
            .Select(candidate => candidate.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count() != candidateEntries.Length)
        {
            throw new ArgumentException(
                "Staff switch candidate names must be unique.",
                nameof(candidates));
        }

        BaseCastLines = baseCastLines;
        Candidates = candidateEntries;
        Policy = policy ?? StaffEquipmentPolicy.Default;
    }

    public int BaseCastLines { get; }

    public ImmutableArray<StaffCandidate> Candidates { get; }

    public StaffEquipmentPolicy Policy { get; }
}
