using SleepHunter.Runtime.Actions;

namespace SleepHunter.Runtime.Automation.Panels;

public sealed record PanelTransitionState
{
    private PanelTransitionState(
        ClientPanel targetPanel,
        PanelTransitionStatus status,
        int attempt,
        int maximumAttempts,
        ClientActionId? actionId)
    {
        TargetPanel = targetPanel;
        Status = status;
        Attempt = attempt;
        MaximumAttempts = maximumAttempts;
        ActionId = actionId;
    }

    public ClientPanel TargetPanel { get; }

    public PanelTransitionStatus Status { get; private init; }

    public int Attempt { get; }

    public int MaximumAttempts { get; }

    public ClientActionId? ActionId { get; }

    internal static PanelTransitionState Pending(
        ClientPanel targetPanel,
        int attempt,
        int maximumAttempts,
        ClientActionId actionId) =>
        new(
            targetPanel,
            PanelTransitionStatus.Pending,
            attempt,
            maximumAttempts,
            actionId);

    internal static PanelTransitionState Succeeded(
        ClientPanel targetPanel,
        int attempt,
        int maximumAttempts,
        ClientActionId? actionId) =>
        new(
            targetPanel,
            PanelTransitionStatus.Succeeded,
            attempt,
            maximumAttempts,
            actionId);

    internal PanelTransitionState TimedOut() =>
        this with { Status = PanelTransitionStatus.TimedOut };

    internal PanelTransitionState IssueFailed() =>
        this with { Status = PanelTransitionStatus.IssueFailed };

    internal PanelTransitionState Cancelled() =>
        this with { Status = PanelTransitionStatus.Cancelled };
}
