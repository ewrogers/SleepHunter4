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
        ClientActionId? actionId,
        int completedEquipmentAttempts,
        bool? targetInventoryExpanded)
    {
        if (attempt < 0 || maximumAttempts < attempt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(attempt),
                attempt,
                "Staff switch attempts must fit within the attempt budget.");
        }

        if (completedEquipmentAttempts < 0 ||
            completedEquipmentAttempts > maximumAttempts)
        {
            throw new ArgumentOutOfRangeException(
                nameof(completedEquipmentAttempts),
                completedEquipmentAttempts,
                "Completed equipment attempts must fit within the attempt budget.");
        }

        if ((status == StaffSwitchStatus.ChangingInventoryMode) !=
            targetInventoryExpanded.HasValue)
        {
            throw new ArgumentException(
                "Only an inventory mode transition can have a target display mode.",
                nameof(targetInventoryExpanded));
        }

        Selection = selection;
        Status = status;
        AttemptTimeout = attemptTimeout;
        Attempt = attempt;
        MaximumAttempts = maximumAttempts;
        ActionId = actionId;
        CompletedEquipmentAttempts = completedEquipmentAttempts;
        TargetInventoryExpanded = targetInventoryExpanded;
    }

    public StaffSelection? Selection { get; }

    public StaffSwitchStatus Status { get; private init; }

    public TimeSpan AttemptTimeout { get; }

    public int Attempt { get; }

    public int MaximumAttempts { get; }

    public ClientActionId? ActionId { get; }

    public int CompletedEquipmentAttempts { get; }

    public bool? TargetInventoryExpanded { get; private init; }

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
            actionId: null,
            completedEquipmentAttempts: completedAttempts,
            targetInventoryExpanded: null);

    internal static StaffSwitchState ChangingInventoryMode(
        StaffSelection selection,
        TimeSpan attemptTimeout,
        int completedEquipmentAttempts,
        int maximumEquipmentAttempts,
        ClientActionId actionId,
        bool targetInventoryExpanded) =>
        new(
            selection,
            StaffSwitchStatus.ChangingInventoryMode,
            attemptTimeout,
            attempt: 1,
            maximumEquipmentAttempts,
            actionId,
            completedEquipmentAttempts,
            targetInventoryExpanded);

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
            actionId,
            completedEquipmentAttempts: attempt - 1,
            targetInventoryExpanded: null);

    internal static StaffSwitchState NoChange(StaffSelection selection) =>
        new(
            selection,
            StaffSwitchStatus.NoChange,
            TimeSpan.Zero,
            attempt: 0,
            maximumAttempts: 0,
            actionId: null,
            completedEquipmentAttempts: 0,
            targetInventoryExpanded: null);

    internal static StaffSwitchState SnapshotUnavailable() =>
        new(
            selection: null,
            StaffSwitchStatus.SnapshotUnavailable,
            TimeSpan.Zero,
            attempt: 0,
            maximumAttempts: 0,
            actionId: null,
            completedEquipmentAttempts: 0,
            targetInventoryExpanded: null);

    internal StaffSwitchState Succeeded() =>
        this with
        {
            Status = StaffSwitchStatus.Succeeded,
            TargetInventoryExpanded = null
        };

    internal StaffSwitchState SelectionInvalidated() =>
        this with
        {
            Status = StaffSwitchStatus.SelectionInvalidated,
            TargetInventoryExpanded = null
        };

    internal StaffSwitchState PanelUnavailable() =>
        this with
        {
            Status = StaffSwitchStatus.PanelUnavailable,
            TargetInventoryExpanded = null
        };

    internal StaffSwitchState TimedOut() =>
        this with
        {
            Status = StaffSwitchStatus.TimedOut,
            TargetInventoryExpanded = null
        };

    internal StaffSwitchState IssueFailed() =>
        this with
        {
            Status = StaffSwitchStatus.IssueFailed,
            TargetInventoryExpanded = null
        };

    internal StaffSwitchState Cancelled() =>
        this with
        {
            Status = StaffSwitchStatus.Cancelled,
            TargetInventoryExpanded = null
        };
}
