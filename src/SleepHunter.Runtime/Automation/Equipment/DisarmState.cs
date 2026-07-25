using SleepHunter.Runtime.Actions;

namespace SleepHunter.Runtime.Automation.Equipment;

public sealed record DisarmState
{
    private DisarmState(
        DisarmStatus status,
        TimeSpan attemptTimeout,
        int attempt,
        int maximumAttempts,
        ClientActionId? actionId)
    {
        Status = status;
        AttemptTimeout = attemptTimeout;
        Attempt = attempt;
        MaximumAttempts = maximumAttempts;
        ActionId = actionId;
    }

    public DisarmStatus Status { get; private init; }

    public TimeSpan AttemptTimeout { get; }

    public int Attempt { get; }

    public int MaximumAttempts { get; }

    public ClientActionId? ActionId { get; }

    internal static DisarmState Disarming(
        TimeSpan attemptTimeout,
        int attempt,
        int maximumAttempts,
        ClientActionId actionId) =>
        new(
            DisarmStatus.Disarming,
            attemptTimeout,
            attempt,
            maximumAttempts,
            actionId);

    internal static DisarmState NoChange() =>
        new(
            DisarmStatus.NoChange,
            TimeSpan.Zero,
            attempt: 0,
            maximumAttempts: 0,
            actionId: null);

    internal static DisarmState SnapshotUnavailable() =>
        new(
            DisarmStatus.SnapshotUnavailable,
            TimeSpan.Zero,
            attempt: 0,
            maximumAttempts: 0,
            actionId: null);

    internal DisarmState Succeeded() =>
        this with { Status = DisarmStatus.Succeeded };

    internal DisarmState TimedOut() =>
        this with { Status = DisarmStatus.TimedOut };

    internal DisarmState Cancelled() =>
        this with { Status = DisarmStatus.Cancelled };
}
