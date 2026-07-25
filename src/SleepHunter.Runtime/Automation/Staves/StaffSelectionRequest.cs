using System.Collections.Immutable;
using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Runtime.Automation.Staves;

public sealed record StaffSelectionRequest
{
    public StaffSelectionRequest(
        int baseCastLines,
        CharacterSnapshot character,
        InventorySnapshot inventory,
        EquipmentSnapshot equipment,
        IEnumerable<StaffCandidate> candidates)
    {
        if (baseCastLines < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(baseCastLines),
                baseCastLines,
                "Base cast lines cannot be negative.");
        }

        ArgumentNullException.ThrowIfNull(character);
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(equipment);
        ArgumentNullException.ThrowIfNull(candidates);

        var candidateEntries = candidates.ToImmutableArray();
        if (candidateEntries.Any(candidate => candidate is null))
        {
            throw new ArgumentException(
                "Staff selection candidates cannot contain null values.",
                nameof(candidates));
        }

        if (candidateEntries
            .Select(candidate => candidate.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count() != candidateEntries.Length)
        {
            throw new ArgumentException(
                "Staff selection candidate names must be unique.",
                nameof(candidates));
        }

        BaseCastLines = baseCastLines;
        Character = character;
        Inventory = inventory;
        Equipment = equipment;
        Candidates = candidateEntries;
    }

    public int BaseCastLines { get; }

    public CharacterSnapshot Character { get; }

    public InventorySnapshot Inventory { get; }

    public EquipmentSnapshot Equipment { get; }

    public ImmutableArray<StaffCandidate> Candidates { get; }
}
