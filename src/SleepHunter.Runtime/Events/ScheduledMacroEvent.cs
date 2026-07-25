using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Events;

public sealed record ScheduledMacroEvent
{
    public ScheduledMacroEvent(MacroEvent input, MacroTimestamp dueAt)
    {
        ArgumentNullException.ThrowIfNull(input);

        Input = input;
        DueAt = dueAt;
    }

    public MacroEvent Input { get; }

    public MacroTimestamp DueAt { get; }
}
