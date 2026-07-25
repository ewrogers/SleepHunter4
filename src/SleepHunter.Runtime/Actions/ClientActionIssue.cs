namespace SleepHunter.Runtime.Actions;

public sealed record ClientActionIssue
{
    public ClientActionIssue(
        ClientActionId actionId,
        ClientActionIssueStatus status)
    {
        if (actionId.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(actionId),
                actionId,
                "Client action issues require a valid action identifier.");
        }

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "The client action issue status is not supported.");
        }

        ActionId = actionId;
        Status = status;
    }

    public ClientActionId ActionId { get; }

    public ClientActionIssueStatus Status { get; }

    public bool WasIssued => Status == ClientActionIssueStatus.Issued;
}
