using System.Collections.Immutable;

using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Snapshots;

public sealed class ClientRosterSnapshot
{
    public static ClientRosterSnapshot Empty { get; } = new();

    private ClientRosterSnapshot()
    {
        Sequence = null;
        CapturedAt = null;
        Clients = [];
    }

    public ClientRosterSnapshot(
        ClientRosterSequence sequence,
        MacroTimestamp capturedAt,
        IEnumerable<ClientRosterEntry> clients)
    {
        ArgumentNullException.ThrowIfNull(clients);

        var entries = clients.ToImmutableArray();
        if (entries.Any(entry => entry is null))
        {
            throw new ArgumentException(
                "Client roster snapshots cannot contain null entries.",
                nameof(clients));
        }

        if (entries
            .Select(entry => entry.Client.InstanceId)
            .Distinct(StringComparer.Ordinal)
            .Count() != entries.Length)
        {
            throw new ArgumentException(
                "Client roster identifiers must be unique.",
                nameof(clients));
        }

        if (entries
            .Select(entry => entry.CharacterName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count() != entries.Length)
        {
            throw new ArgumentException(
                "Client roster character names must be unique.",
                nameof(clients));
        }

        Sequence = sequence;
        CapturedAt = capturedAt;
        Clients = entries;
    }

    public ClientRosterSequence? Sequence { get; }

    public MacroTimestamp? CapturedAt { get; }

    public ImmutableArray<ClientRosterEntry> Clients { get; }

    public bool HasObservation => Sequence is not null;
}
