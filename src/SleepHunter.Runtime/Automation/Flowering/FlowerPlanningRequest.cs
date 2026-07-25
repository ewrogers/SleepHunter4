using System.Collections.Immutable;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Automation.Flowering;

public sealed record FlowerPlanningRequest
{
    public FlowerPlanningRequest(
        ClientIdentity sourceClient,
        MapLocationSnapshot? sourceLocation,
        FlowerQueueState queue,
        FlowerScheduleState? schedules,
        IEnumerable<ClientRosterEntry> clients,
        MacroTimestamp currentTime,
        FlowerTargetPolicy? policy = null)
    {
        ArgumentNullException.ThrowIfNull(sourceClient);
        ArgumentNullException.ThrowIfNull(queue);
        ArgumentNullException.ThrowIfNull(clients);

        var observations = clients.ToImmutableArray();
        if (observations.Any(observation => observation is null))
        {
            throw new ArgumentException(
                "Flower client observations cannot contain null values.",
                nameof(clients));
        }

        if (observations
            .Select(observation => observation.Client.InstanceId)
            .Distinct(StringComparer.Ordinal)
            .Count() != observations.Length)
        {
            throw new ArgumentException(
                "Flower client observation identifiers must be unique.",
                nameof(clients));
        }

        if (observations
            .Select(observation => observation.CharacterName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count() != observations.Length)
        {
            throw new ArgumentException(
                "Flower client observation names must be unique.",
                nameof(clients));
        }

        SourceClient = sourceClient;
        SourceLocation = sourceLocation;
        Queue = queue;
        Schedules = schedules ?? FlowerScheduleState.Empty;
        Clients = observations;
        CurrentTime = currentTime;
        Policy = policy ?? FlowerTargetPolicy.Default;
    }

    public ClientIdentity SourceClient { get; }

    public MapLocationSnapshot? SourceLocation { get; }

    public FlowerQueueState Queue { get; }

    public FlowerScheduleState Schedules { get; }

    public ImmutableArray<ClientRosterEntry> Clients { get; }

    public MacroTimestamp CurrentTime { get; }

    public FlowerTargetPolicy Policy { get; }
}
