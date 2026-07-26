using SleepHunter.Runtime.Automation.Panels;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Snapshots;

public sealed record ClientSnapshot
{
    public ClientSnapshot(
        SnapshotSequence sequence,
        MacroTimestamp captureStartedAt,
        MacroTimestamp captureCompletedAt,
        ClientIdentity client,
        SnapshotQuality quality,
        ClientPresence presence,
        ClientPanel activePanel = ClientPanel.Unknown,
        CharacterSnapshot? character = null,
        InventorySnapshot? inventory = null,
        EquipmentSnapshot? equipment = null,
        VitalsSnapshot? vitals = null,
        SpellbookSnapshot? spellbook = null,
        SkillbookSnapshot? skillbook = null,
        MapLocationSnapshot? location = null,
        bool isInventoryExpanded = false,
        bool isMinimizedMode = false,
        bool isChatOpen = false,
        GroupSnapshot? group = null,
        ActiveSpellEffectsSnapshot? activeSpellEffects = null,
        WorldEntitiesSnapshot? worldEntities = null,
        MessageDialogsSnapshot? messageDialogs = null)
    {
        ArgumentNullException.ThrowIfNull(client);

        if (sequence.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sequence),
                sequence,
                "Snapshot sequences must be positive.");
        }

        if (captureCompletedAt < captureStartedAt)
        {
            throw new ArgumentException(
                "Snapshot capture cannot complete before it starts.",
                nameof(captureCompletedAt));
        }

        if (!Enum.IsDefined(activePanel))
        {
            throw new ArgumentOutOfRangeException(
                nameof(activePanel),
                activePanel,
                "The observed client panel is not supported.");
        }

        Sequence = sequence;
        CaptureStartedAt = captureStartedAt;
        CaptureCompletedAt = captureCompletedAt;
        Client = client;
        Quality = quality;
        Presence = presence;
        ActivePanel = activePanel;
        Character = character;
        Inventory = inventory;
        Equipment = equipment;
        Vitals = vitals;
        Spellbook = spellbook;
        Skillbook = skillbook;
        Location = location;
        IsInventoryExpanded = isInventoryExpanded;
        IsPanelExpanded = isInventoryExpanded;
        IsMinimizedMode = isMinimizedMode;
        IsChatOpen = isChatOpen;
        Group = group;
        ActiveSpellEffects = activeSpellEffects;
        WorldEntities = worldEntities;
        MessageDialogs = messageDialogs ?? MessageDialogsSnapshot.Empty;
    }

    public SnapshotSequence Sequence { get; }

    public MacroTimestamp CaptureStartedAt { get; }

    public MacroTimestamp CaptureCompletedAt { get; }

    public ClientIdentity Client { get; }

    public SnapshotQuality Quality { get; }

    public ClientPresence Presence { get; }

    public ClientPanel ActivePanel { get; }

    public CharacterSnapshot? Character { get; }

    public InventorySnapshot? Inventory { get; }

    public EquipmentSnapshot? Equipment { get; }

    public VitalsSnapshot? Vitals { get; }

    public SpellbookSnapshot? Spellbook { get; }

    public SkillbookSnapshot? Skillbook { get; }

    public MapLocationSnapshot? Location { get; }

    public bool IsInventoryExpanded { get; }

    public bool IsPanelExpanded { get; }

    public bool IsMinimizedMode { get; }

    public bool IsChatOpen { get; }

    public GroupSnapshot? Group { get; }

    public ActiveSpellEffectsSnapshot? ActiveSpellEffects { get; }

    public WorldEntitiesSnapshot? WorldEntities { get; }

    public MessageDialogsSnapshot MessageDialogs { get; }

    public bool IsPopupOpen => MessageDialogs.Count > 0;

    public bool IsUsable => Quality == SnapshotQuality.Complete;
}
