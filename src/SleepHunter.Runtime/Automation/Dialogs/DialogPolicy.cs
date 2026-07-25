namespace SleepHunter.Runtime.Automation.Dialogs;

public sealed record DialogPolicy
{
    public static DialogPolicy Default { get; } = new(
        TimeSpan.FromSeconds(2),
        TimeSpan.FromMilliseconds(50));

    public DialogPolicy(TimeSpan closeDelay, TimeSpan actionDuration)
    {
        if (closeDelay <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(closeDelay),
                closeDelay,
                "Dialog close delays must be positive.");
        }

        if (actionDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(actionDuration),
                actionDuration,
                "Dialog close action durations must be positive.");
        }

        CloseDelay = closeDelay;
        ActionDuration = actionDuration;
    }

    public TimeSpan CloseDelay { get; }

    public TimeSpan ActionDuration { get; }
}
