using SleepHunter.Runtime.Actions;

namespace SleepHunter.Runtime.Automation.Staves;

public sealed record StaffSwitchState
{
    private StaffSwitchState(
        StaffSelection? selection,
        StaffSwitchStatus status,
        TimeSpan attemptTimeout,
        int attempt,
        int maximumAttempts,
        ClientActionId? actionId)
    {
        Selection = selection;
        Status = status;
        AttemptTimeout = attemptTimeout;
        Attempt = attempt;
        MaximumAttempts = maximumAttempts;
        ActionId = actionId;
    }

    public StaffSelection? Selection { get; }

    public StaffSwitchStatus Status { get; private init; }

    public TimeSpan AttemptTimeout { get; }

    public int Attempt { get; }

    public int MaximumAttempts { get; }

    public ClientActionId? ActionId { get; }

    internal static StaffSwitchState WaitingForInventory(
        StaffSelection selection,
        StaffEquipmentPolicy policy,
        int completedAttempts) =>
        new(
            selection,
            StaffSwitchStatus.WaitingForInventory,
            policy.AttemptTimeout,
            completedAttempts,
            policy.MaximumAttempts,
            actionId: null);

    internal static StaffSwitchState ChangingWeapon(
        StaffSelection selection,
        TimeSpan attemptTimeout,
        int attempt,
        int maximumAttempts,
        ClientActionId actionId) =>
        new(
            selection,
            StaffSwitchStatus.ChangingWeapon,
            attemptTimeout,
            attempt,
            maximumAttempts,
            actionId);

    internal static StaffSwitchState NoChange(StaffSelection selection) =>
        new(
            selection,
            StaffSwitchStatus.NoChange,
            TimeSpan.Zero,
            attempt: 0,
            maximumAttempts: 0,
            actionId: null);

    internal static StaffSwitchState SnapshotUnavailable() =>
        new(
            selection: null,
            StaffSwitchStatus.SnapshotUnavailable,
            TimeSpan.Zero,
            attempt: 0,
            maximumAttempts: 0,
            actionId: null);

    internal StaffSwitchState Succeeded() =>
        this with { Status = StaffSwitchStatus.Succeeded };

    internal StaffSwitchState SelectionInvalidated() =>
        this with { Status = StaffSwitchStatus.SelectionInvalidated };

    internal StaffSwitchState PanelUnavailable() =>
        this with { Status = StaffSwitchStatus.PanelUnavailable };

    internal StaffSwitchState TimedOut() =>
        this with { Status = StaffSwitchStatus.TimedOut };

    internal StaffSwitchState Cancelled() =>
        this with { Status = StaffSwitchStatus.Cancelled };
}
