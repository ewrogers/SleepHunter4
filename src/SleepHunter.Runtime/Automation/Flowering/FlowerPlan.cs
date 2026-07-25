using System.Collections.Immutable;
using SleepHunter.Runtime.Automation.Spells;
using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Runtime.Automation.Flowering;

public sealed record FlowerPlan
{
    internal FlowerPlan(
        FlowerPlanStatus status,
        FlowerSelectionKind? selectionKind,
        FlowerQueueEntry? selectedEntry,
        ClientRosterEntry? selectedClient,
        SpellTarget? selectedTarget,
        FlowerQueueState queue,
        FlowerScheduleState schedules,
        ImmutableArray<FlowerReadiness> readiness,
        ImmutableArray<FlowerClientReadiness> clientReadiness)
    {
        Status = status;
        SelectionKind = selectionKind;
        SelectedEntry = selectedEntry;
        SelectedClient = selectedClient;
        SelectedTarget = selectedTarget;
        Queue = queue;
        Schedules = schedules;
        Readiness = readiness;
        ClientReadiness = clientReadiness;
    }

    public FlowerPlanStatus Status { get; }

    public FlowerSelectionKind? SelectionKind { get; }

    public FlowerQueueEntry? SelectedEntry { get; }

    public ClientRosterEntry? SelectedClient { get; }

    public SpellTarget? SelectedTarget { get; }

    public FlowerQueueState Queue { get; }

    public FlowerScheduleState Schedules { get; }

    public ImmutableArray<FlowerReadiness> Readiness { get; }

    public ImmutableArray<FlowerClientReadiness> ClientReadiness { get; }

    public bool HasSelection => SelectedTarget is not null;
}
