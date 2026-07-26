namespace SleepHunter.Runtime.Automation.Spells;

public sealed record TargetLocation(
    TargetLocationStatus Status,
    SpellTarget? Target)
{
    public bool IsResolved =>
        Status == TargetLocationStatus.Resolved &&
        Target is not null;
}
