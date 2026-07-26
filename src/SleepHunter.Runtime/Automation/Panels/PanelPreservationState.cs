namespace SleepHunter.Runtime.Automation.Panels;

public sealed record PanelPreservationState
{
    private PanelPreservationState(
        ClientPanel originalPanel,
        PanelTransitionPolicy transition,
        PanelPreservationStatus status)
    {
        if (!Enum.IsDefined(originalPanel) ||
            originalPanel == ClientPanel.Unknown)
        {
            throw new ArgumentOutOfRangeException(
                nameof(originalPanel),
                originalPanel,
                "Panel preservation requires a known original panel.");
        }

        ArgumentNullException.ThrowIfNull(transition);

        OriginalPanel = originalPanel;
        Transition = transition;
        Status = status;
    }

    public ClientPanel OriginalPanel { get; }

    public PanelTransitionPolicy Transition { get; }

    public PanelPreservationStatus Status { get; private init; }

    public bool IsActive =>
        Status is
            PanelPreservationStatus.Tracking or
            PanelPreservationStatus.Restoring;

    internal static PanelPreservationState Tracking(
        ClientPanel originalPanel,
        PanelTransitionPolicy transition) =>
        new(
            originalPanel,
            transition,
            PanelPreservationStatus.Tracking);

    internal PanelPreservationState Restoring() =>
        this with { Status = PanelPreservationStatus.Restoring };

    internal PanelPreservationState Succeeded() =>
        this with { Status = PanelPreservationStatus.Succeeded };

    internal PanelPreservationState TimedOut() =>
        this with { Status = PanelPreservationStatus.TimedOut };

    internal PanelPreservationState IssueFailed() =>
        this with { Status = PanelPreservationStatus.IssueFailed };

    internal PanelPreservationState Cancelled() =>
        this with { Status = PanelPreservationStatus.Cancelled };
}
