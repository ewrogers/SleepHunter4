using SleepHunter.Runtime.Actions;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Automation.Dialogs;

public sealed record DialogState
{
    private DialogState(
        DialogStatus status,
        DialogPolicy policy,
        MacroTimestamp? dueAt,
        ClientActionId? actionId,
        MacroTimestamp? completesAt)
    {
        Status = status;
        Policy = policy;
        DueAt = dueAt;
        ActionId = actionId;
        CompletesAt = completesAt;
    }

    public DialogStatus Status { get; private init; }

    public DialogPolicy Policy { get; }

    public MacroTimestamp? DueAt { get; }

    public ClientActionId? ActionId { get; }

    public MacroTimestamp? CompletesAt { get; }

    internal static DialogState Scheduled(
        DialogPolicy policy,
        MacroTimestamp dueAt) =>
        new(
            DialogStatus.Scheduled,
            policy,
            dueAt,
            actionId: null,
            completesAt: null);

    internal DialogState Rescheduled(MacroTimestamp dueAt) =>
        new(
            DialogStatus.Scheduled,
            Policy,
            dueAt,
            actionId: null,
            completesAt: null);

    internal DialogState Closing(
        ClientActionId actionId,
        MacroTimestamp completesAt) =>
        new(
            DialogStatus.Closing,
            Policy,
            DueAt,
            actionId,
            completesAt);

    internal DialogState Closed() =>
        this with { Status = DialogStatus.Closed };

    internal DialogState IssueFailed() =>
        this with { Status = DialogStatus.IssueFailed };

    internal DialogState Cancelled() =>
        this with { Status = DialogStatus.Cancelled };
}
