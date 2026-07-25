namespace SleepHunter.Runtime.Automation.Spells;

public sealed record TargetResolution(
    SpellTarget Target,
    int SelectedIndex,
    int NextIndex,
    int PointCount);
