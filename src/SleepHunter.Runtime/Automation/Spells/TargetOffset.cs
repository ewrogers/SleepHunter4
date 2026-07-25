namespace SleepHunter.Runtime.Automation.Spells;

public readonly record struct TargetOffset(int X, int Y)
{
    public static TargetOffset Zero { get; } = new(0, 0);
}
