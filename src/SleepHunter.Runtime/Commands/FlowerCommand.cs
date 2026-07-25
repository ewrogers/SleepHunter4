using SleepHunter.Runtime.Automation.Flowering;

namespace SleepHunter.Runtime.Commands;

public sealed record FlowerCommand : MacroCommand
{
    public FlowerCommand(
        FlowerExecutionPolicy? policy = null,
        FlowerStaffCatalog? staffCatalog = null)
    {
        Policy = policy ?? FlowerExecutionPolicy.Default;
        StaffCatalog = staffCatalog ?? FlowerStaffCatalog.Empty;
    }

    public FlowerExecutionPolicy Policy { get; }

    public FlowerStaffCatalog StaffCatalog { get; }
}
