using SleepHunter.Runtime.Characters;

namespace SleepHunter.Runtime.Snapshots;

public sealed record CharacterSnapshot
{
    public CharacterSnapshot(
        CharacterClass characterClass,
        int level,
        int abilityLevel)
    {
        if (!Enum.IsDefined(characterClass))
        {
            throw new ArgumentOutOfRangeException(
                nameof(characterClass),
                characterClass,
                "The observed character class is not supported.");
        }

        if (level < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(level),
                level,
                "Character level cannot be negative.");
        }

        if (abilityLevel < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(abilityLevel),
                abilityLevel,
                "Character ability level cannot be negative.");
        }

        Class = characterClass;
        Level = level;
        AbilityLevel = abilityLevel;
    }

    public CharacterClass Class { get; }

    public int Level { get; }

    public int AbilityLevel { get; }
}
