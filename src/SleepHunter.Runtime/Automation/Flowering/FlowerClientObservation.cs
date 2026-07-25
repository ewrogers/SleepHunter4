using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Automation.Flowering;

public sealed record FlowerClientObservation
{
    public FlowerClientObservation(
        ClientIdentity client,
        string characterName,
        ClientPresence presence,
        bool isMacroRunning,
        bool isWaitingForMana,
        MapLocationSnapshot? location,
        VitalsSnapshot? vitals,
        MacroTimestamp? lastFloweredAt = null)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(characterName);

        if (!Enum.IsDefined(presence))
        {
            throw new ArgumentOutOfRangeException(
                nameof(presence),
                presence,
                "The observed client presence is not supported.");
        }

        Client = client;
        CharacterName = characterName.Trim();
        Presence = presence;
        IsMacroRunning = isMacroRunning;
        IsWaitingForMana = isWaitingForMana;
        Location = location;
        Vitals = vitals;
        LastFloweredAt = lastFloweredAt;
    }

    public ClientIdentity Client { get; }

    public string CharacterName { get; }

    public ClientPresence Presence { get; }

    public bool IsMacroRunning { get; }

    public bool IsWaitingForMana { get; }

    public MapLocationSnapshot? Location { get; }

    public VitalsSnapshot? Vitals { get; }

    public MacroTimestamp? LastFloweredAt { get; }
}
