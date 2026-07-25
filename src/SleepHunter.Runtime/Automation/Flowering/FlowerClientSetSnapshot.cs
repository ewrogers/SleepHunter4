using System.Collections.Immutable;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Automation.Flowering;

public sealed class FlowerClientSetSnapshot
{
    public static FlowerClientSetSnapshot Empty { get; } = new();

    private FlowerClientSetSnapshot()
    {
        Sequence = null;
        CapturedAt = null;
        Clients = [];
    }

    public FlowerClientSetSnapshot(
        FlowerObservationSequence sequence,
        MacroTimestamp capturedAt,
        IEnumerable<FlowerClientObservation> clients)
    {
        ArgumentNullException.ThrowIfNull(clients);

        var observations = clients.ToImmutableArray();
        if (observations.Any(observation => observation is null))
        {
            throw new ArgumentException(
                "Flower client snapshots cannot contain null observations.",
                nameof(clients));
        }

        if (observations
            .Select(observation => observation.Client.InstanceId)
            .Distinct(StringComparer.Ordinal)
            .Count() != observations.Length)
        {
            throw new ArgumentException(
                "Flower client snapshot identifiers must be unique.",
                nameof(clients));
        }

        if (observations
            .Select(observation => observation.CharacterName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count() != observations.Length)
        {
            throw new ArgumentException(
                "Flower client snapshot names must be unique.",
                nameof(clients));
        }

        Sequence = sequence;
        CapturedAt = capturedAt;
        Clients = observations;
    }

    public FlowerObservationSequence? Sequence { get; }

    public MacroTimestamp? CapturedAt { get; }

    public ImmutableArray<FlowerClientObservation> Clients { get; }

    public bool HasObservation => Sequence is not null;
}
