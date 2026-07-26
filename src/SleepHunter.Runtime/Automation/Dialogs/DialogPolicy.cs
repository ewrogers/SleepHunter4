namespace SleepHunter.Runtime.Automation.Dialogs;

public sealed record DialogPolicy
{
    public static DialogPolicy Default { get; } = new(
        TimeSpan.FromSeconds(2),
        TimeSpan.FromMilliseconds(50));

    public DialogPolicy(
        TimeSpan observationTimeout,
        TimeSpan actionDuration)
    {
        if (observationTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(observationTimeout),
                observationTimeout,
                "Dialog observation timeouts must be positive.");
        }

        if (actionDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(actionDuration),
                actionDuration,
                "Dialog close action durations must be positive.");
        }

        ObservationTimeout = observationTimeout;
        ActionDuration = actionDuration;
    }

    public TimeSpan ObservationTimeout { get; }

    public TimeSpan ActionDuration { get; }
}
