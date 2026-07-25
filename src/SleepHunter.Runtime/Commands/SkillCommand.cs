using SleepHunter.Runtime.Automation.Skills;

namespace SleepHunter.Runtime.Commands;

public sealed record UseNextSkillCommand : MacroCommand
{
    public UseNextSkillCommand(SkillExecutionPolicy? policy = null)
    {
        Policy = policy ?? SkillExecutionPolicy.Default;
    }

    public SkillExecutionPolicy Policy { get; }
}
