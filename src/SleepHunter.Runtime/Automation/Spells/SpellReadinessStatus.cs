namespace SleepHunter.Runtime.Automation.Spells;

public enum SpellReadinessStatus
{
    Missing,
    Ready,
    WaitingForHealth,
    WaitingForMana,
    CoolingDown,
    Complete,
    TargetLevelUnavailable
}
