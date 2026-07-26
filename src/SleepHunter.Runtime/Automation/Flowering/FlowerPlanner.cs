using System.Collections.Immutable;
using SleepHunter.Runtime.Automation.Spells;
using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Runtime.Automation.Flowering;

public static class FlowerPlanner
{
    public static FlowerPlan Plan(FlowerPlanningRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var schedules = request.Schedules.Synchronize(
            request.Queue,
            request.CurrentTime);
        var readiness = request.Queue.Entries
            .Select(entry => EvaluateEntry(entry, schedules, request))
            .ToImmutableArray();
        var clientReadiness = request.Policy.AutoFlowerWaitingCharacters
            ? request.Clients
                .Select(client => EvaluateClient(client, request))
                .ToImmutableArray()
            : ImmutableArray<FlowerClientReadiness>.Empty;
        var waitingClient = SelectWaitingClient(clientReadiness);

        if (request.Policy.PrioritizeAlternateCharacters &&
            waitingClient is not null)
        {
            return SelectWaitingClient(
                request,
                schedules,
                readiness,
                clientReadiness,
                waitingClient);
        }

        var availability = readiness.ToDictionary(
            entry => entry.Entry.Id,
            ToQueueAvailability);
        var queueEvaluation = request.Queue.EvaluateNext(
            availability,
            request.Policy.PrioritizeAlternateCharacters);
        if (queueEvaluation.SelectedEntry is { } selectedEntry)
        {
            var selected = readiness.Single(
                entry => entry.Entry.Id == selectedEntry.Id);
            return new FlowerPlan(
                FlowerPlanStatus.Ready,
                FlowerSelectionKind.QueueEntry,
                selectedEntry,
                selected.TargetClient,
                selectedEntry.Target,
                queueEvaluation.State,
                schedules,
                readiness,
                clientReadiness);
        }

        if (waitingClient is not null)
        {
            return SelectWaitingClient(
                request,
                schedules,
                readiness,
                clientReadiness,
                waitingClient);
        }

        var status =
            request.Queue.Entries.IsEmpty &&
            !request.Policy.AutoFlowerWaitingCharacters
                ? FlowerPlanStatus.Idle
                : FlowerPlanStatus.Waiting;
        return new FlowerPlan(
            status,
            selectionKind: null,
            selectedEntry: null,
            selectedClient: null,
            selectedTarget: null,
            request.Queue,
            schedules,
            readiness,
            clientReadiness);
    }

    private static FlowerReadiness EvaluateEntry(
        FlowerQueueEntry entry,
        FlowerScheduleState schedules,
        FlowerPlanningRequest request)
    {
        var targetClient = FindTargetClient(entry, request.Clients);
        var targetStatus = EvaluateTarget(
            entry.Target,
            targetClient,
            request.SourceLocation,
            request.Policy);
        var readyAt = schedules.GetReadyAt(entry.Id);
        if (targetStatus is not null)
        {
            return new FlowerReadiness(
                entry,
                targetClient,
                targetStatus.Value,
                readyAt);
        }

        var intervalReady =
            entry.Interval is not null &&
            readyAt <= request.CurrentTime;
        var manaReady =
            entry.ManaThreshold is { } threshold &&
            targetClient?.Vitals is { } vitals &&
            vitals.CurrentMana < threshold;
        if (intervalReady || manaReady)
        {
            return new FlowerReadiness(
                entry,
                targetClient,
                FlowerReadinessStatus.Ready,
                readyAt);
        }

        var status = (entry.Interval, entry.ManaThreshold) switch
        {
            (not null, not null) =>
                FlowerReadinessStatus.WaitingForCondition,
            (not null, null) =>
                FlowerReadinessStatus.WaitingForInterval,
            (null, not null) =>
                FlowerReadinessStatus.WaitingForMana,
            _ => throw new InvalidOperationException(
                "Flower entries require at least one readiness condition.")
        };
        return new FlowerReadiness(
            entry,
            targetClient,
            status,
            readyAt);
    }

    private static FlowerReadinessStatus? EvaluateTarget(
        SpellTarget target,
        ClientRosterEntry? targetClient,
        MapLocationSnapshot? sourceLocation,
        FlowerTargetPolicy policy)
    {
        switch (target.Kind)
        {
            case SpellTargetKind.Self:
            case SpellTargetKind.ScreenPoint:
                return null;

            case SpellTargetKind.Character:
                if (targetClient is not
                    {
                        Presence: ClientPresence.InWorld
                    })
                {
                    return FlowerReadinessStatus.TargetUnavailable;
                }

                if (sourceLocation is null ||
                    targetClient.Location is null)
                {
                    return FlowerReadinessStatus.LocationUnavailable;
                }

                return sourceLocation.IsWithinRange(
                    targetClient.Location,
                    policy.MaximumXDistance,
                    policy.MaximumYDistance)
                    ? null
                    : FlowerReadinessStatus.OutOfRange;

            case SpellTargetKind.RelativeTile:
            case SpellTargetKind.RelativeArea:
                if (sourceLocation is null)
                {
                    return FlowerReadinessStatus.LocationUnavailable;
                }

                return Math.Abs(target.X!.Value) <=
                       policy.MaximumXDistance &&
                       Math.Abs(target.Y!.Value) <=
                       policy.MaximumYDistance
                    ? null
                    : FlowerReadinessStatus.OutOfRange;

            case SpellTargetKind.AbsoluteTile:
            case SpellTargetKind.AbsoluteArea:
                if (sourceLocation is null)
                {
                    return FlowerReadinessStatus.LocationUnavailable;
                }

                return Math.Abs(sourceLocation.X - target.X!.Value) <=
                       policy.MaximumXDistance &&
                       Math.Abs(sourceLocation.Y - target.Y!.Value) <=
                       policy.MaximumYDistance
                    ? null
                    : FlowerReadinessStatus.OutOfRange;

            case SpellTargetKind.None:
            default:
                return FlowerReadinessStatus.TargetUnavailable;
        }
    }

    private static FlowerClientReadiness EvaluateClient(
        ClientRosterEntry client,
        FlowerPlanningRequest request)
    {
        var status = GetClientReadinessStatus(client, request);
        return new FlowerClientReadiness(client, status);
    }

    private static FlowerClientReadinessStatus GetClientReadinessStatus(
        ClientRosterEntry client,
        FlowerPlanningRequest request)
    {
        if (string.Equals(
                client.Client.InstanceId,
                request.SourceClient.InstanceId,
                StringComparison.Ordinal))
        {
            return FlowerClientReadinessStatus.SourceClient;
        }

        if (client.Presence != ClientPresence.InWorld)
        {
            return FlowerClientReadinessStatus.LoggedOut;
        }

        if (!client.IsMacroRunning)
        {
            return FlowerClientReadinessStatus.MacroStopped;
        }

        if (!client.IsWaitingForMana)
        {
            return FlowerClientReadinessStatus.NotWaitingForMana;
        }

        if (request.SourceLocation is null || client.Location is null)
        {
            return FlowerClientReadinessStatus.LocationUnavailable;
        }

        return request.SourceLocation.IsWithinRange(
            client.Location,
            request.Policy.MaximumXDistance,
            request.Policy.MaximumYDistance)
            ? FlowerClientReadinessStatus.Ready
            : FlowerClientReadinessStatus.OutOfRange;
    }

    private static ClientRosterEntry? SelectWaitingClient(
        ImmutableArray<FlowerClientReadiness> readiness) =>
        readiness
            .Where(entry =>
                entry.Status == FlowerClientReadinessStatus.Ready)
            .Select(entry => entry.Client)
            .OrderBy(client => client.LastFloweredAt is null ? 0 : 1)
            .ThenBy(client => client.LastFloweredAt)
            .ThenBy(
                client => client.CharacterName,
                StringComparer.OrdinalIgnoreCase)
            .ThenBy(
                client => client.Client.InstanceId,
                StringComparer.Ordinal)
            .FirstOrDefault();

    private static FlowerPlan SelectWaitingClient(
        FlowerPlanningRequest request,
        FlowerScheduleState schedules,
        ImmutableArray<FlowerReadiness> readiness,
        ImmutableArray<FlowerClientReadiness> clientReadiness,
        ClientRosterEntry selectedClient) =>
        new(
            FlowerPlanStatus.Ready,
            FlowerSelectionKind.WaitingCharacter,
            selectedEntry: null,
            selectedClient,
            SpellTarget.Character(selectedClient.CharacterName),
            request.Queue,
            schedules,
            readiness,
            clientReadiness);

    private static ClientRosterEntry? FindTargetClient(
        FlowerQueueEntry entry,
        ImmutableArray<ClientRosterEntry> clients)
    {
        if (entry.Target.Kind != SpellTargetKind.Character)
        {
            return null;
        }

        return clients.FirstOrDefault(
            client => string.Equals(
                client.CharacterName,
                entry.Target.CharacterName,
                StringComparison.OrdinalIgnoreCase));
    }

    private static FlowerQueueAvailability ToQueueAvailability(
        FlowerReadiness readiness) =>
        readiness.Status switch
        {
            FlowerReadinessStatus.Ready =>
                FlowerQueueAvailability.Ready,
            FlowerReadinessStatus.WaitingForInterval or
                FlowerReadinessStatus.WaitingForMana or
                FlowerReadinessStatus.WaitingForCondition =>
                FlowerQueueAvailability.Waiting,
            _ => FlowerQueueAvailability.Unavailable
        };
}
