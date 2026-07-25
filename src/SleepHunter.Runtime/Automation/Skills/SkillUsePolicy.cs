namespace SleepHunter.Runtime.Automation.Skills;

public sealed record SkillUsePolicy
{
    public static SkillUsePolicy Default { get; } = new();

    public SkillUsePolicy(
        bool requireMana = true,
        AssailMode assailMode = AssailMode.SpaceBar,
        bool disarmForAssails = true)
    {
        if (!Enum.IsDefined(assailMode))
        {
            throw new ArgumentOutOfRangeException(
                nameof(assailMode),
                assailMode,
                "The assail mode is not supported.");
        }

        RequireMana = requireMana;
        AssailMode = assailMode;
        DisarmForAssails = disarmForAssails;
    }

    public bool RequireMana { get; }

    public AssailMode AssailMode { get; }

    public bool DisarmForAssails { get; }
}
