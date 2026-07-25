using SleepHunter.Runtime.Automation.Spells;

namespace SleepHunter.Runtime.Commands;

public sealed record CastNextSpellCommand : MacroCommand
{
    public CastNextSpellCommand(SpellExecutionPolicy? policy = null)
    {
        Policy = policy ?? SpellExecutionPolicy.Default;
    }

    public SpellExecutionPolicy Policy { get; }
}
