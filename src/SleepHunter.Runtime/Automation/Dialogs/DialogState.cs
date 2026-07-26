using SleepHunter.Runtime.Actions;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Automation.Dialogs;

public sealed record DialogState
{
    private DialogState(
        DialogStatus status,
        DialogPolicy policy,
        MacroTimestamp? dueAt,
        ClientActionId? actionId,
        MacroTimestamp? completesAt,
        SnapshotSequence? lastCancelSnapshotSequence,
        MacroTimestamp? snapshotRequiredAfter)
    {
        Status = status;
        Policy = policy;
        DueAt = dueAt;
        ActionId = actionId;
        CompletesAt = completesAt;
        LastCancelSnapshotSequence = lastCancelSnapshotSequence;
        SnapshotRequiredAfter = snapshotRequiredAfter;
    }

    public DialogStatus Status { get; private init; }

    public DialogPolicy Policy { get; }

    public MacroTimestamp? DueAt { get; }

    public ClientActionId? ActionId { get; }

    public MacroTimestamp? CompletesAt { get; }

    public SnapshotSequence? LastCancelSnapshotSequence { get; }

    public MacroTimestamp? SnapshotRequiredAfter { get; }

    internal static DialogState Scheduled(
        DialogPolicy policy,
        MacroTimestamp dueAt) =>
        new(
            DialogStatus.Scheduled,
            policy,
            dueAt,
            actionId: null,
            completesAt: null,
            lastCancelSnapshotSequence: null,
            snapshotRequiredAfter: null);

    internal DialogState Rescheduled(MacroTimestamp dueAt) =>
        new(
            DialogStatus.Scheduled,
            Policy,
            dueAt,
            actionId: null,
            completesAt: null,
            lastCancelSnapshotSequence: LastCancelSnapshotSequence,
            snapshotRequiredAfter: null);

    internal DialogState Closing(
        ClientActionId actionId,
        MacroTimestamp completesAt,
        SnapshotSequence observedSnapshotSequence) =>
        new(
            DialogStatus.Closing,
            Policy,
            DueAt,
            actionId,
            completesAt,
            observedSnapshotSequence,
            snapshotRequiredAfter: null);

    internal DialogState AwaitingObservation(
        MacroTimestamp snapshotRequiredAfter) =>
        new(
            DialogStatus.AwaitingObservation,
            Policy,
            DueAt,
            actionId: null,
            completesAt: null,
            lastCancelSnapshotSequence: LastCancelSnapshotSequence,
            snapshotRequiredAfter: snapshotRequiredAfter);

    internal DialogState Closed() =>
        new(
            DialogStatus.Closed,
            Policy,
            DueAt,
            actionId: null,
            completesAt: null,
            lastCancelSnapshotSequence: LastCancelSnapshotSequence,
            snapshotRequiredAfter: SnapshotRequiredAfter);

    internal DialogState IssueFailed() =>
        new(
            DialogStatus.IssueFailed,
            Policy,
            DueAt,
            actionId: null,
            completesAt: null,
            lastCancelSnapshotSequence: LastCancelSnapshotSequence,
            snapshotRequiredAfter: SnapshotRequiredAfter);

    internal DialogState Cancelled() =>
        new(
            DialogStatus.Cancelled,
            Policy,
            DueAt,
            actionId: null,
            completesAt: null,
            lastCancelSnapshotSequence: LastCancelSnapshotSequence,
            snapshotRequiredAfter: SnapshotRequiredAfter);
}
