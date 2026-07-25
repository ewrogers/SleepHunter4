using SleepHunter.Runtime.Actions;

namespace SleepHunter.Interop.Input;

public enum ClientIntentPlanStatus
{
    Planned,
    Rejected,
    Unsupported
}

public enum ClientIntentPlanFailure
{
    None,
    ClientMismatch,
    SnapshotUnavailable,
    ClientNotInWorld,
    PanelMismatch,
    InventoryModeMismatch,
    InventoryItemMismatch,
    SpellMismatch,
    TargetUnavailable,
    TargetOutOfRange,
    UnsupportedTarget,
    AlreadySatisfied,
    UnsupportedIntent,
    InputUnavailable,
    CoordinateOutOfBounds
}

public sealed record ClientIntentPlanResult
{
    private ClientIntentPlanResult(
        ClientActionId actionId,
        ClientIntentPlanStatus status,
        WindowInputPlan? plan,
        ClientIntentPlanFailure failure,
        string message)
    {
        if (actionId.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(actionId),
                actionId,
                "Client intent planning requires a valid action identifier.");
        }

        ActionId = actionId;
        Status = status;
        Plan = plan;
        Failure = failure;
        Message = message;
    }

    public ClientActionId ActionId { get; }

    public ClientIntentPlanStatus Status { get; }

    public WindowInputPlan? Plan { get; }

    public ClientIntentPlanFailure Failure { get; }

    public string Message { get; }

    public bool IsPlanned => Status == ClientIntentPlanStatus.Planned;

    public static ClientIntentPlanResult Planned(
        ClientActionId actionId,
        WindowInputPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        return new ClientIntentPlanResult(
            actionId,
            ClientIntentPlanStatus.Planned,
            plan,
            ClientIntentPlanFailure.None,
            string.Empty);
    }

    public static ClientIntentPlanResult Rejected(
        ClientActionId actionId,
        ClientIntentPlanFailure failure,
        string message) =>
        CreateFailure(
            actionId,
            ClientIntentPlanStatus.Rejected,
            failure,
            message);

    public static ClientIntentPlanResult Unsupported(
        ClientActionId actionId,
        ClientIntentPlanFailure failure,
        string message) =>
        CreateFailure(
            actionId,
            ClientIntentPlanStatus.Unsupported,
            failure,
            message);

    private static ClientIntentPlanResult CreateFailure(
        ClientActionId actionId,
        ClientIntentPlanStatus status,
        ClientIntentPlanFailure failure,
        string message)
    {
        if (!Enum.IsDefined(failure) ||
            failure == ClientIntentPlanFailure.None)
        {
            throw new ArgumentException(
                "An unsuccessful intent plan requires a failure reason.",
                nameof(failure));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        return new ClientIntentPlanResult(
            actionId,
            status,
            plan: null,
            failure,
            message.Trim());
    }
}
