using SleepHunter.Runtime.Automation.WaterBeds;

namespace SleepHunter.Runtime.Commands;

public sealed record UseWaterBedCommand : MacroCommand
{
    public UseWaterBedCommand(WaterBedPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        Policy = policy;
    }

    public WaterBedPolicy Policy { get; }
}
