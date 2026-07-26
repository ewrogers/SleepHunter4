using SleepHunter.Runtime.Characters;
using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Runtime.Automation.Staves;

public sealed record StaffCandidate
{
    public StaffCandidate(
        string name,
        CharacterClass? requiredClass,
        int requiredLevel,
        int requiredAbilityLevel,
        int castLines)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (requiredClass == CharacterClass.Unknown ||
            requiredClass is { } value && !Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(
                nameof(requiredClass),
                requiredClass,
                "Staff class requirements must be known or class-neutral.");
        }

        if (requiredLevel < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(requiredLevel),
                requiredLevel,
                "Required level cannot be negative.");
        }

        if (requiredAbilityLevel < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(requiredAbilityLevel),
                requiredAbilityLevel,
                "Required ability level cannot be negative.");
        }

        if (castLines < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(castLines),
                castLines,
                "Staff cast lines cannot be negative.");
        }

        Name = name.Trim();
        RequiredClass = requiredClass;
        RequiredLevel = requiredLevel;
        RequiredAbilityLevel = requiredAbilityLevel;
        CastLines = castLines;
    }

    public string Name { get; }

    public CharacterClass? RequiredClass { get; }

    public int RequiredLevel { get; }

    public int RequiredAbilityLevel { get; }

    public int CastLines { get; }

    public bool IsClassNeutral => RequiredClass is null;

    public bool UsesAbilityLevelRequirement => RequiredAbilityLevel > 0;

    public int RequiredProgressionLevel =>
        UsesAbilityLevelRequirement
            ? RequiredAbilityLevel
            : RequiredLevel;

    public bool IsEligibleFor(CharacterSnapshot character)
    {
        ArgumentNullException.ThrowIfNull(character);

        var classCompatible = RequiredClass is null ||
                              (character.Class != CharacterClass.Unknown &&
                               RequiredClass == character.Class);
        var progressionCompatible = UsesAbilityLevelRequirement
            ? RequiredAbilityLevel <= character.AbilityLevel
            : RequiredLevel <= character.Level;

        return classCompatible && progressionCompatible;
    }
}
