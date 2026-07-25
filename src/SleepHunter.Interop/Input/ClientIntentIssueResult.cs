using SleepHunter.Runtime.Actions;

namespace SleepHunter.Interop.Input;

public enum ClientIntentIssueStatus
{
    Issued,
    Rejected,
    Unsupported,
    Failed,
    PartiallyIssued
}

public sealed record ClientIntentIssueResult
{
    public ClientIntentIssueResult(
        ClientActionId actionId,
        ClientIntentIssueStatus status,
        ClientIntentPlanResult plan,
        WindowInputDispatchResult? dispatch = null)
    {
        if (actionId.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(actionId),
                actionId,
                "Client intent issuance requires a valid action identifier.");
        }

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "The client intent issuance status is not supported.");
        }

        ArgumentNullException.ThrowIfNull(plan);

        if (actionId != plan.ActionId)
        {
            throw new ArgumentException(
                "The issuance and plan action identifiers must match.",
                nameof(plan));
        }

        var expectsDispatch = plan.IsPlanned;
        if (expectsDispatch != (dispatch is not null))
        {
            throw new ArgumentException(
                "Planned input requires a dispatch result, while unsuccessful planning cannot dispatch.",
                nameof(dispatch));
        }

        if (plan.Status == ClientIntentPlanStatus.Rejected &&
            status != ClientIntentIssueStatus.Rejected)
        {
            throw new ArgumentException(
                "A rejected plan requires a rejected issuance.",
                nameof(plan));
        }

        if ((status == ClientIntentIssueStatus.Unsupported) !=
            (plan.Status == ClientIntentPlanStatus.Unsupported))
        {
            throw new ArgumentException(
                "Unsupported issuance must match an unsupported plan.",
                nameof(plan));
        }

        if (dispatch is not null &&
            !DoesDispatchStatusMatch(status, dispatch.Status))
        {
            throw new ArgumentException(
                "The intent issuance and input dispatch statuses must match.",
                nameof(dispatch));
        }

        ActionId = actionId;
        Status = status;
        Plan = plan;
        Dispatch = dispatch;
    }

    public ClientActionId ActionId { get; }

    public ClientIntentIssueStatus Status { get; }

    public ClientIntentPlanResult Plan { get; }

    public WindowInputDispatchResult? Dispatch { get; }

    private static bool DoesDispatchStatusMatch(
        ClientIntentIssueStatus issueStatus,
        WindowInputDispatchStatus dispatchStatus) =>
        (issueStatus, dispatchStatus) switch
        {
            (
                ClientIntentIssueStatus.Issued,
                WindowInputDispatchStatus.Issued) => true,
            (
                ClientIntentIssueStatus.Rejected,
                WindowInputDispatchStatus.Rejected) => true,
            (
                ClientIntentIssueStatus.Failed,
                WindowInputDispatchStatus.Failed) => true,
            (
                ClientIntentIssueStatus.PartiallyIssued,
                WindowInputDispatchStatus.PartiallyIssued) => true,
            _ => false
        };
}
