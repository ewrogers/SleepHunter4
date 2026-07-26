using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Automation.Flowering;

public sealed record FlowerSchedule(
    TimeSpan Interval,
    MacroTimestamp ReadyAt);
