using SleepHunter.Runtime.Automation.Spells;

namespace SleepHunter.Runtime.Commands;

public sealed record CastNextSpellCommand : MacroCommand
{
    public CastNextSpellCommand(
        SpellExecutionPolicy? policy = null,
        SpellStaffCatalog? staffCatalog = null)
    {
        Policy = policy ?? SpellExecutionPolicy.Default;
        StaffCatalog = staffCatalog ?? SpellStaffCatalog.Empty;
    }

    public SpellExecutionPolicy Policy { get; }

    public SpellStaffCatalog StaffCatalog { get; }
}
