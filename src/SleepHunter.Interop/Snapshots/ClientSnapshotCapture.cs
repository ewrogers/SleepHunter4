using System.Collections.Immutable;
using System.Text;
using SleepHunter.Interop.Mappings;
using SleepHunter.Interop.Memory;
using SleepHunter.Runtime.Automation.Panels;
using SleepHunter.Runtime.Characters;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Interop.Snapshots;

public sealed partial class ClientSnapshotCapture : IClientSnapshotCapture
{
    private const string WorldUserFuncKey = "WorldUserFunc";
    private const string CharacterNameKey = "CharacterName";
    private const string CharacterIdKey = "CharacterId";
    private const string CharacterClassKey = "CharacterClass";
    private const string UserStateKey = "UserState";
    private const string PrivilegeLevelKey = "PrivilegeLevel";
    private const string LevelKey = "Level";
    private const string AbilityLevelKey = "AbilityLevel";
    private const string GoldKey = "Gold";
    private const string TotalExperienceKey = "TotalExperience";
    private const string StrengthKey = "Strength";
    private const string DexterityKey = "Dexterity";
    private const string WisdomKey = "Wisdom";
    private const string ConstitutionKey = "Constitution";
    private const string IntelligenceKey = "Intelligence";
    private const string StatPointsKey = "StatPoints";
    private const string ExperienceToNextLevelKey = "ExperienceToNextLevel";
    private const string GamePointsKey = "GamePoints";
    private const string AbilityToNextLevelKey = "AbilityToNextLevel";
    private const string TotalAbilityKey = "TotalAbility";
    private const string WeightKey = "Weight";
    private const string MaximumWeightKey = "MaximumWeight";
    private const string ArmorClassKey = "ArmorClass";
    private const string DamageModifierKey = "DamageModifier";
    private const string HitModifierKey = "HitModifier";
    private const string AttackElementKey = "AttackElement";
    private const string DefenseElementKey = "DefenseElement";
    private const string MagicResistanceKey = "MagicResistance";
    private const string ActionStateKey = "ActionState";
    private const string ShowAbilityMetadataKey = "ShowAbilityMetadata";
    private const string ShowMasterMetadataKey = "ShowMasterMetadata";
    private const string CurrentHealthKey = "CurrentHealth";
    private const string MaximumHealthKey = "MaximumHealth";
    private const string CurrentManaKey = "CurrentMana";
    private const string MaximumManaKey = "MaximumMana";
    private const string ActivePanelKey = "ActivePanel";
    private const string InventoryExpandedKey = "InventoryExpanded";
    private const string UserChattingKey = "UserChatting";
    private const string MapNumberKey = "MapNumber";
    private const string MapNameKey = "MapName";
    private const string MapXKey = "MapX";
    private const string MapYKey = "MapY";
    private const string MapWidthKey = "MapWidth";
    private const string MapHeightKey = "MapHeight";
    private const string MapFlagsKey = "MapFlags";
    private const string MapWeatherKey = "MapWeather";
    private const string MapTransferActiveKey = "MapTransferActive";
    private const string InventoryKey = "Inventory";
    private const string InventoryPanesKey = "InventoryPanes";
    private const string EquipmentKey = "Equipment";
    private const string EquipmentSnapshotKey = "EquipmentSnapshot";
    private const string SkillbookKey = "Skillbook";
    private const string SpellbookKey = "Spellbook";
    private const string SkillbookPanesKey = "SkillbookPanes";
    private const string SkillbookPaneCapacityKey = "SkillbookPaneCapacity";
    private const string SpellbookPanesKey = "SpellbookPanes";
    private const string SpellbookPaneCapacityKey = "SpellbookPaneCapacity";
    private const string GroupMemberCacheKey = "GroupMemberCache";
    private const string GroupMemberCountKey = "GroupMemberCount";
    private const string ActiveSpellEffectsKey = "ActiveSpellEffects";
    private const string WorldObjectListKey = "WorldObjectList";

    private static readonly Encoding StrictAscii = Encoding.GetEncoding(
        Encoding.ASCII.CodePage,
        EncoderFallback.ExceptionFallback,
        DecoderFallback.ExceptionFallback);

    private static readonly ImmutableArray<RequiredVariable> RequiredVariables =
    [
        new(WorldUserFuncKey, MemoryValueKind.Unsigned32),
        new(CharacterNameKey, MemoryValueKind.Text),
        new(CharacterIdKey, MemoryValueKind.Unsigned32),
        new(CharacterClassKey, MemoryValueKind.Byte),
        new(UserStateKey, MemoryValueKind.Unsigned32),
        new(PrivilegeLevelKey, MemoryValueKind.Signed32),
        new(LevelKey, MemoryValueKind.Byte),
        new(AbilityLevelKey, MemoryValueKind.Byte),
        new(GoldKey, MemoryValueKind.Unsigned32),
        new(TotalExperienceKey, MemoryValueKind.Unsigned32),
        new(StrengthKey, MemoryValueKind.Unsigned16),
        new(DexterityKey, MemoryValueKind.Unsigned16),
        new(WisdomKey, MemoryValueKind.Unsigned16),
        new(ConstitutionKey, MemoryValueKind.Unsigned16),
        new(IntelligenceKey, MemoryValueKind.Unsigned16),
        new(StatPointsKey, MemoryValueKind.Unsigned16),
        new(ExperienceToNextLevelKey, MemoryValueKind.Unsigned32),
        new(GamePointsKey, MemoryValueKind.Unsigned32),
        new(AbilityToNextLevelKey, MemoryValueKind.Unsigned32),
        new(TotalAbilityKey, MemoryValueKind.Unsigned32),
        new(WeightKey, MemoryValueKind.Unsigned32),
        new(MaximumWeightKey, MemoryValueKind.Unsigned32),
        new(ArmorClassKey, MemoryValueKind.SByte),
        new(DamageModifierKey, MemoryValueKind.Byte),
        new(HitModifierKey, MemoryValueKind.Byte),
        new(AttackElementKey, MemoryValueKind.Unsigned16),
        new(DefenseElementKey, MemoryValueKind.Unsigned16),
        new(MagicResistanceKey, MemoryValueKind.Unsigned16),
        new(ActionStateKey, MemoryValueKind.Byte),
        new(ShowAbilityMetadataKey, MemoryValueKind.Unsigned32),
        new(ShowMasterMetadataKey, MemoryValueKind.Unsigned32),
        new(CurrentHealthKey, MemoryValueKind.Unsigned32),
        new(MaximumHealthKey, MemoryValueKind.Unsigned32),
        new(CurrentManaKey, MemoryValueKind.Unsigned32),
        new(MaximumManaKey, MemoryValueKind.Unsigned32),
        new(ActivePanelKey, MemoryValueKind.Byte),
        new(InventoryExpandedKey, MemoryValueKind.Byte),
        new(UserChattingKey, MemoryValueKind.Byte),
        new(MapNumberKey, MemoryValueKind.Unsigned32),
        new(MapNameKey, MemoryValueKind.Text),
        new(MapXKey, MemoryValueKind.Signed32),
        new(MapYKey, MemoryValueKind.Signed32),
        new(MapWidthKey, MemoryValueKind.Signed32),
        new(MapHeightKey, MemoryValueKind.Signed32),
        new(MapFlagsKey, MemoryValueKind.Unsigned32),
        new(MapWeatherKey, MemoryValueKind.Byte),
        new(MapTransferActiveKey, MemoryValueKind.Byte),
        new(InventoryKey, MemoryValueKind.Binary),
        new(InventoryPanesKey, MemoryValueKind.Binary),
        new(EquipmentKey, MemoryValueKind.Binary),
        new(EquipmentSnapshotKey, MemoryValueKind.Binary),
        new(SkillbookKey, MemoryValueKind.Binary),
        new(SpellbookKey, MemoryValueKind.Binary),
        new(SkillbookPanesKey, MemoryValueKind.Binary),
        new(SkillbookPaneCapacityKey, MemoryValueKind.Signed32),
        new(SpellbookPanesKey, MemoryValueKind.Binary),
        new(SpellbookPaneCapacityKey, MemoryValueKind.Signed32),
        new(GroupMemberCacheKey, MemoryValueKind.Binary),
        new(GroupMemberCountKey, MemoryValueKind.Unsigned32),
        new(ActiveSpellEffectsKey, MemoryValueKind.Binary),
        new(WorldObjectListKey, MemoryValueKind.Unsigned32)
    ];

    private readonly ClientIdentity client;
    private readonly ClientMemoryMap map;
    private readonly IProcessMemorySource source;
    private readonly MemoryReadLimits limits;
    private readonly MacroClock clock;
    private readonly AbilitySnapshotCatalog abilityCatalog;
    private LocationIdentity? acceptedLocationIdentity;
    private LocationIdentity? pendingLocationIdentity;
    private int captureInProgress;

    public ClientSnapshotCapture(
        ClientIdentity client,
        ClientMemoryMap map,
        IProcessMemorySource source,
        MemoryReadLimits limits,
        MacroClock clock,
        AbilitySnapshotCatalog? abilityCatalog = null)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(limits);
        ArgumentNullException.ThrowIfNull(clock);

        if (map.PointerWidth != limits.PointerWidth)
        {
            throw new ArgumentException(
                "The memory map and capture limits pointer widths must match.",
                nameof(limits));
        }

        ValidateSchema(map);

        this.client = client;
        this.map = map;
        this.source = source;
        this.limits = limits;
        this.clock = clock;
        this.abilityCatalog = abilityCatalog ?? AbilitySnapshotCatalog.Empty;
    }

    public ClientIdentity Client => client;

    public SnapshotCaptureResult Capture(
        SnapshotSequence sequence,
        SnapshotCaptureSections sections = SnapshotCaptureSections.Core)
    {
        if (sequence.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sequence),
                sequence,
                "Snapshot sequences must be positive.");
        }

        if ((sections & ~SnapshotCaptureSections.All) != 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sections),
                sections,
                "The requested snapshot sections are not supported.");
        }

        var startedAt = clock.GetCurrentTimestamp();
        if (Interlocked.CompareExchange(
                ref captureInProgress,
                1,
                comparand: 0) != 0)
        {
            var completedAt = clock.GetCurrentTimestamp();
            return new SnapshotCaptureResult(
                snapshot: null,
                SnapshotQuality.Unknown,
                new SnapshotCaptureError(
                    SnapshotSection.Capture,
                    SnapshotCaptureFailure.CaptureAlreadyInProgress,
                    "A snapshot capture is already running for this client."),
                new SnapshotCaptureMetrics(
                    sequence,
                    startedAt,
                    completedAt,
                    ImmutableArray<SnapshotSectionMetrics>.Empty,
                    EmptyReadMetrics()));
        }

        try
        {
            return ApplyLocationCoherence(
                CaptureCore(sequence, sections, startedAt));
        }
        finally
        {
            Volatile.Write(ref captureInProgress, 0);
        }
    }

    private SnapshotCaptureResult ApplyLocationCoherence(
        SnapshotCaptureResult result)
    {
        if (!result.Succeeded)
        {
            pendingLocationIdentity = null;
            if (acceptedLocationIdentity is not null &&
                result.Error is
                {
                    Failure: SnapshotCaptureFailure.MappingReadFailed,
                    VariableKey: MapNameKey
                } mapNameError)
            {
                return LocationTransition(
                    result,
                    MapNameKey,
                    "The client map name is changing during a map transition.",
                    mapNameError.ReadError,
                    markCoherenceFailed: false);
            }

            return result;
        }

        var snapshot = result.Snapshot!;
        if (snapshot.Presence != ClientPresence.InWorld ||
            snapshot.Location is not { } location)
        {
            acceptedLocationIdentity = null;
            pendingLocationIdentity = null;
            return result;
        }

        var candidate = new LocationIdentity(
            location.MapNumber,
            location.MapName);
        if (acceptedLocationIdentity is not { } accepted)
        {
            acceptedLocationIdentity = candidate;
            return result;
        }

        if (candidate == accepted)
        {
            pendingLocationIdentity = null;
            return result;
        }

        if (pendingLocationIdentity is { } pending &&
            pending.MapNumber == candidate.MapNumber &&
            (pending == candidate ||
             !string.Equals(
                 candidate.MapName,
                 accepted.MapName,
                 StringComparison.Ordinal)))
        {
            acceptedLocationIdentity = candidate;
            pendingLocationIdentity = null;
            return result;
        }

        pendingLocationIdentity = candidate;
        return LocationTransition(
            result,
            MapNumberKey,
            "The client map identity is changing and requires another coherent observation.",
            readError: null,
            markCoherenceFailed: true);
    }

    private static SnapshotCaptureResult LocationTransition(
        SnapshotCaptureResult result,
        string variableKey,
        string message,
        MappedMemoryReadError? readError,
        bool markCoherenceFailed)
    {
        var metrics = markCoherenceFailed
            ? MarkCoherenceFailed(result.Metrics)
            : result.Metrics;
        return new SnapshotCaptureResult(
            snapshot: null,
            SnapshotQuality.Incoherent,
            new SnapshotCaptureError(
                SnapshotSection.Coherence,
                SnapshotCaptureFailure.LocationTransition,
                message,
                variableKey,
                readError),
            metrics);
    }

    private static SnapshotCaptureMetrics MarkCoherenceFailed(
        SnapshotCaptureMetrics metrics)
    {
        var sections = metrics.Sections
            .Select(
                section => section.Section == SnapshotSection.Coherence
                    ? new SnapshotSectionMetrics(
                        section.Section,
                        section.Duration,
                        succeeded: false,
                        section.Reads)
                    : section)
            .ToImmutableArray();
        return new SnapshotCaptureMetrics(
            metrics.Sequence,
            metrics.CaptureStartedAt,
            metrics.CaptureCompletedAt,
            sections,
            metrics.Reads);
    }

    private SnapshotCaptureResult CaptureCore(
        SnapshotSequence sequence,
        SnapshotCaptureSections requestedSections,
        MacroTimestamp startedAt)
    {
        var session = new MemoryReadSession(source, limits);
        var reader = new MappedMemoryReader(map, session);
        var sections = ImmutableArray.CreateBuilder<SnapshotSectionMetrics>();

        var sectionStartedAt = clock.GetCurrentTimestamp();
        var readsBefore = session.Metrics;
        var presenceSucceeded = TryReadPresence(
            reader,
            out var presence,
            out var error,
            out var failureQuality);
        var sectionCompletedAt = clock.GetCurrentTimestamp();
        AddSection(
            sections,
            SnapshotSection.Presence,
            sectionStartedAt,
            sectionCompletedAt,
            readsBefore,
            session.Metrics,
            presenceSucceeded);
        if (!presenceSucceeded)
        {
            return Failure(
                sequence,
                startedAt,
                session,
                sections,
                failureQuality,
                error!);
        }

        if (presence.Presence == ClientPresence.LoggedOut)
        {
            return Success(
                sequence,
                startedAt,
                session,
                sections,
                ClientPresence.LoggedOut,
                ClientPanel.Unknown,
                character: null,
                vitals: null,
                location: null);
        }

        sectionStartedAt = clock.GetCurrentTimestamp();
        readsBefore = session.Metrics;
        var characterSucceeded = TryReadCharacter(
            reader,
            presence,
            out var character,
            out error,
            out failureQuality);
        sectionCompletedAt = clock.GetCurrentTimestamp();
        AddSection(
            sections,
            SnapshotSection.Character,
            sectionStartedAt,
            sectionCompletedAt,
            readsBefore,
            session.Metrics,
            characterSucceeded);
        if (!characterSucceeded)
        {
            return Failure(
                sequence,
                startedAt,
                session,
                sections,
                failureQuality,
                error!);
        }

        sectionStartedAt = clock.GetCurrentTimestamp();
        readsBefore = session.Metrics;
        var vitalsSucceeded = TryReadVitals(
            reader,
            out var vitals,
            out error,
            out failureQuality);
        sectionCompletedAt = clock.GetCurrentTimestamp();
        AddSection(
            sections,
            SnapshotSection.Vitals,
            sectionStartedAt,
            sectionCompletedAt,
            readsBefore,
            session.Metrics,
            vitalsSucceeded);
        if (!vitalsSucceeded)
        {
            return Failure(
                sequence,
                startedAt,
                session,
                sections,
                failureQuality,
                error!);
        }

        sectionStartedAt = clock.GetCurrentTimestamp();
        readsBefore = session.Metrics;
        var clientStateSucceeded = TryReadClientState(
            reader,
            out var activePanel,
            out var isInventoryExpanded,
            out var isUserChatting,
            out error);
        sectionCompletedAt = clock.GetCurrentTimestamp();
        AddSection(
            sections,
            SnapshotSection.ClientState,
            sectionStartedAt,
            sectionCompletedAt,
            readsBefore,
            session.Metrics,
            clientStateSucceeded);
        if (!clientStateSucceeded)
        {
            return Failure(
                sequence,
                startedAt,
                session,
                sections,
                SnapshotQuality.Partial,
                error!);
        }

        sectionStartedAt = clock.GetCurrentTimestamp();
        readsBefore = session.Metrics;
        var locationSucceeded = TryReadLocation(
            reader,
            out var location,
            out error,
            out failureQuality);
        sectionCompletedAt = clock.GetCurrentTimestamp();
        AddSection(
            sections,
            SnapshotSection.Location,
            sectionStartedAt,
            sectionCompletedAt,
            readsBefore,
            session.Metrics,
            locationSucceeded);
        if (!locationSucceeded)
        {
            return Failure(
                sequence,
                startedAt,
                session,
                sections,
                failureQuality,
                error!);
        }

        InventorySnapshot? inventory = null;
        if (requestedSections.HasFlag(SnapshotCaptureSections.Inventory))
        {
            sectionStartedAt = clock.GetCurrentTimestamp();
            readsBefore = session.Metrics;
            var inventorySucceeded = TryReadInventory(
                reader,
                out inventory,
                out error,
                out failureQuality);
            sectionCompletedAt = clock.GetCurrentTimestamp();
            AddSection(
                sections,
                SnapshotSection.Inventory,
                sectionStartedAt,
                sectionCompletedAt,
                readsBefore,
                session.Metrics,
                inventorySucceeded);
            if (!inventorySucceeded)
            {
                return Failure(
                    sequence,
                    startedAt,
                    session,
                    sections,
                    failureQuality,
                    error!);
            }
        }

        EquipmentSnapshot? equipment = null;
        if (requestedSections.HasFlag(SnapshotCaptureSections.Equipment))
        {
            sectionStartedAt = clock.GetCurrentTimestamp();
            readsBefore = session.Metrics;
            var equipmentSucceeded = TryReadEquipment(
                reader,
                out equipment,
                out error,
                out failureQuality);
            sectionCompletedAt = clock.GetCurrentTimestamp();
            AddSection(
                sections,
                SnapshotSection.Equipment,
                sectionStartedAt,
                sectionCompletedAt,
                readsBefore,
                session.Metrics,
                equipmentSucceeded);
            if (!equipmentSucceeded)
            {
                return Failure(
                    sequence,
                    startedAt,
                    session,
                    sections,
                    failureQuality,
                    error!);
            }
        }

        SkillbookSnapshot? skillbook = null;
        if (requestedSections.HasFlag(SnapshotCaptureSections.Skillbook))
        {
            sectionStartedAt = clock.GetCurrentTimestamp();
            readsBefore = session.Metrics;
            var skillbookSucceeded = TryReadSkillbook(
                reader,
                out skillbook,
                out error,
                out failureQuality);
            sectionCompletedAt = clock.GetCurrentTimestamp();
            AddSection(
                sections,
                SnapshotSection.Skillbook,
                sectionStartedAt,
                sectionCompletedAt,
                readsBefore,
                session.Metrics,
                skillbookSucceeded);
            if (!skillbookSucceeded)
            {
                return Failure(
                    sequence,
                    startedAt,
                    session,
                    sections,
                    failureQuality,
                    error!);
            }
        }

        SpellbookSnapshot? spellbook = null;
        if (requestedSections.HasFlag(SnapshotCaptureSections.Spellbook))
        {
            sectionStartedAt = clock.GetCurrentTimestamp();
            readsBefore = session.Metrics;
            var spellbookSucceeded = TryReadSpellbook(
                reader,
                out spellbook,
                out error,
                out failureQuality);
            sectionCompletedAt = clock.GetCurrentTimestamp();
            AddSection(
                sections,
                SnapshotSection.Spellbook,
                sectionStartedAt,
                sectionCompletedAt,
                readsBefore,
                session.Metrics,
                spellbookSucceeded);
            if (!spellbookSucceeded)
            {
                return Failure(
                    sequence,
                    startedAt,
                    session,
                    sections,
                    failureQuality,
                    error!);
            }
        }

        GroupSnapshot? group = null;
        if (requestedSections.HasFlag(SnapshotCaptureSections.Group))
        {
            sectionStartedAt = clock.GetCurrentTimestamp();
            readsBefore = session.Metrics;
            var groupSucceeded = TryReadGroup(
                reader,
                out group,
                out error,
                out failureQuality);
            sectionCompletedAt = clock.GetCurrentTimestamp();
            AddSection(
                sections,
                SnapshotSection.Group,
                sectionStartedAt,
                sectionCompletedAt,
                readsBefore,
                session.Metrics,
                groupSucceeded);
            if (!groupSucceeded)
            {
                return Failure(
                    sequence,
                    startedAt,
                    session,
                    sections,
                    failureQuality,
                    error!);
            }
        }

        ActiveSpellEffectsSnapshot? activeSpellEffects = null;
        if (requestedSections.HasFlag(
                SnapshotCaptureSections.ActiveSpellEffects))
        {
            sectionStartedAt = clock.GetCurrentTimestamp();
            readsBefore = session.Metrics;
            var effectsSucceeded = TryReadActiveSpellEffects(
                reader,
                out activeSpellEffects,
                out error,
                out failureQuality);
            sectionCompletedAt = clock.GetCurrentTimestamp();
            AddSection(
                sections,
                SnapshotSection.ActiveSpellEffects,
                sectionStartedAt,
                sectionCompletedAt,
                readsBefore,
                session.Metrics,
                effectsSucceeded);
            if (!effectsSucceeded)
            {
                return Failure(
                    sequence,
                    startedAt,
                    session,
                    sections,
                    failureQuality,
                    error!);
            }
        }

        WorldEntitiesSnapshot? worldEntities = null;
        if (requestedSections.HasFlag(
                SnapshotCaptureSections.WorldEntities))
        {
            sectionStartedAt = clock.GetCurrentTimestamp();
            readsBefore = session.Metrics;
            var entitiesSucceeded = TryReadWorldEntities(
                reader,
                character!.CharacterId,
                out worldEntities,
                out error,
                out failureQuality);
            sectionCompletedAt = clock.GetCurrentTimestamp();
            AddSection(
                sections,
                SnapshotSection.WorldEntities,
                sectionStartedAt,
                sectionCompletedAt,
                readsBefore,
                session.Metrics,
                entitiesSucceeded);
            if (!entitiesSucceeded)
            {
                return Failure(
                    sequence,
                    startedAt,
                    session,
                    sections,
                    failureQuality,
                    error!);
            }
        }

        sectionStartedAt = clock.GetCurrentTimestamp();
        readsBefore = session.Metrics;
        var coherenceSucceeded = TryValidateCoherence(
            reader,
            presence.SessionAddress,
            character!.CharacterId,
            activePanel,
            isInventoryExpanded,
            isUserChatting,
            location!,
            out error,
            out failureQuality);
        sectionCompletedAt = clock.GetCurrentTimestamp();
        AddSection(
            sections,
            SnapshotSection.Coherence,
            sectionStartedAt,
            sectionCompletedAt,
            readsBefore,
            session.Metrics,
            coherenceSucceeded);
        if (!coherenceSucceeded)
        {
            return Failure(
                sequence,
                startedAt,
                session,
                sections,
                failureQuality,
                error!);
        }

        return Success(
            sequence,
            startedAt,
            session,
            sections,
            ClientPresence.InWorld,
            activePanel,
            character,
            vitals,
            location,
            inventory,
            equipment,
            skillbook,
            spellbook,
            isInventoryExpanded,
            isUserChatting,
            group,
            activeSpellEffects,
            worldEntities);
    }

    private SnapshotCaptureResult Success(
        SnapshotSequence sequence,
        MacroTimestamp startedAt,
        MemoryReadSession session,
        ImmutableArray<SnapshotSectionMetrics>.Builder sections,
        ClientPresence presence,
        ClientPanel activePanel,
        CharacterSnapshot? character,
        VitalsSnapshot? vitals,
        MapLocationSnapshot? location,
        InventorySnapshot? inventory = null,
        EquipmentSnapshot? equipment = null,
        SkillbookSnapshot? skillbook = null,
        SpellbookSnapshot? spellbook = null,
        bool isInventoryExpanded = false,
        bool isUserChatting = false,
        GroupSnapshot? group = null,
        ActiveSpellEffectsSnapshot? activeSpellEffects = null,
        WorldEntitiesSnapshot? worldEntities = null)
    {
        var completedAt = clock.GetCurrentTimestamp();
        var snapshot = new ClientSnapshot(
            sequence,
            startedAt,
            completedAt,
            client,
            SnapshotQuality.Complete,
            presence,
            activePanel,
            character,
            inventory,
            equipment,
            vitals: vitals,
            spellbook: spellbook,
            skillbook: skillbook,
            location: location,
            isInventoryExpanded: isInventoryExpanded,
            isUserChatting: isUserChatting,
            group: group,
            activeSpellEffects: activeSpellEffects,
            worldEntities: worldEntities);
        var metrics = new SnapshotCaptureMetrics(
            sequence,
            startedAt,
            completedAt,
            sections.ToImmutable(),
            session.Metrics);
        return new SnapshotCaptureResult(
            snapshot,
            SnapshotQuality.Complete,
            error: null,
            metrics);
    }

    private SnapshotCaptureResult Failure(
        SnapshotSequence sequence,
        MacroTimestamp startedAt,
        MemoryReadSession session,
        ImmutableArray<SnapshotSectionMetrics>.Builder sections,
        SnapshotQuality quality,
        SnapshotCaptureError error)
    {
        var completedAt = clock.GetCurrentTimestamp();
        return new SnapshotCaptureResult(
            snapshot: null,
            quality,
            error,
            new SnapshotCaptureMetrics(
                sequence,
                startedAt,
                completedAt,
                sections.ToImmutable(),
                session.Metrics));
    }

    private static bool TryReadPresence(
        MappedMemoryReader reader,
        out PresenceObservation presence,
        out SnapshotCaptureError? error,
        out SnapshotQuality failureQuality)
    {
        if (!reader.TryResolveAddress(
                WorldUserFuncKey,
                out var sessionAddress,
                out var sessionError))
        {
            if (IsNullPointer(sessionError))
            {
                presence = PresenceObservation.LoggedOut;
                error = null;
                failureQuality = SnapshotQuality.Unknown;
                return true;
            }

            presence = default;
            error = MappingFailure(
                SnapshotSection.Presence,
                WorldUserFuncKey,
                sessionError);
            failureQuality = SnapshotQuality.Partial;
            return false;
        }

        if (!reader.TryReadText(
                CharacterNameKey,
                StrictAscii,
                out var name,
                out var nameError,
                requireTerminator: true))
        {
            presence = default;
            error = MappingFailure(
                SnapshotSection.Presence,
                CharacterNameKey,
                nameError);
            failureQuality = SnapshotQuality.Partial;
            return false;
        }

        if (!reader.TryReadByte(
                LevelKey,
                out var level,
                out var levelError))
        {
            presence = default;
            error = MappingFailure(
                SnapshotSection.Presence,
                LevelKey,
                levelError);
            failureQuality = SnapshotQuality.Partial;
            return false;
        }

        if (string.IsNullOrEmpty(name) || level == 0)
        {
            presence = PresenceObservation.LoggedOut;
            error = null;
            failureQuality = SnapshotQuality.Unknown;
            return true;
        }

        if (!IsValidCharacterName(name))
        {
            presence = default;
            error = InvalidValue(
                SnapshotSection.Presence,
                CharacterNameKey,
                "The observed character name is invalid.");
            failureQuality = SnapshotQuality.Incoherent;
            return false;
        }

        presence = new PresenceObservation(
            ClientPresence.InWorld,
            sessionAddress,
            name,
            level);
        error = null;
        failureQuality = SnapshotQuality.Unknown;
        return true;
    }

    private static bool TryReadCharacter(
        MappedMemoryReader reader,
        PresenceObservation presence,
        out CharacterSnapshot? character,
        out SnapshotCaptureError? error,
        out SnapshotQuality failureQuality)
    {
        if (!reader.TryReadByte(
                CharacterClassKey,
                out var rawClass,
                out var classError))
        {
            character = null;
            error = MappingFailure(
                SnapshotSection.Character,
                CharacterClassKey,
                classError);
            failureQuality = SnapshotQuality.Partial;
            return false;
        }

        if (!TryMapCharacterClass(rawClass, out var characterClass))
        {
            character = null;
            error = InvalidValue(
                SnapshotSection.Character,
                CharacterClassKey,
                $"Character class value 0x{rawClass:X2} is not supported.");
            failureQuality = SnapshotQuality.Incoherent;
            return false;
        }

        if (!reader.TryReadByte(
                AbilityLevelKey,
                out var abilityLevel,
                out var abilityError))
        {
            character = null;
            error = MappingFailure(
                SnapshotSection.Character,
                AbilityLevelKey,
                abilityError);
            failureQuality = SnapshotQuality.Partial;
            return false;
        }

        if (!reader.TryReadUInt32(
                CharacterIdKey,
                out var characterId,
                out var idError))
        {
            character = null;
            error = MappingFailure(
                SnapshotSection.Character,
                CharacterIdKey,
                idError);
            failureQuality = SnapshotQuality.Partial;
            return false;
        }

        if (!TryReadCharacterUInt32(
                reader,
                UserStateKey,
                out var rawUserState,
                out error,
                out failureQuality) ||
            !TryReadCharacterInt32(
                reader,
                PrivilegeLevelKey,
                out var privilegeLevel,
                out error,
                out failureQuality) ||
            !TryReadCharacterUInt32(
                reader,
                GoldKey,
                out var gold,
                out error,
                out failureQuality) ||
            !TryReadCharacterUInt32(
                reader,
                TotalExperienceKey,
                out var totalExperience,
                out error,
                out failureQuality) ||
            !TryReadCharacterUInt16(
                reader,
                StrengthKey,
                out var strength,
                out error,
                out failureQuality) ||
            !TryReadCharacterUInt16(
                reader,
                DexterityKey,
                out var dexterity,
                out error,
                out failureQuality) ||
            !TryReadCharacterUInt16(
                reader,
                WisdomKey,
                out var wisdom,
                out error,
                out failureQuality) ||
            !TryReadCharacterUInt16(
                reader,
                ConstitutionKey,
                out var constitution,
                out error,
                out failureQuality) ||
            !TryReadCharacterUInt16(
                reader,
                IntelligenceKey,
                out var intelligence,
                out error,
                out failureQuality) ||
            !TryReadCharacterUInt16(
                reader,
                StatPointsKey,
                out var statPoints,
                out error,
                out failureQuality) ||
            !TryReadCharacterUInt32(
                reader,
                ExperienceToNextLevelKey,
                out var experienceToNextLevel,
                out error,
                out failureQuality) ||
            !TryReadCharacterUInt32(
                reader,
                GamePointsKey,
                out var gamePoints,
                out error,
                out failureQuality) ||
            !TryReadCharacterUInt32(
                reader,
                AbilityToNextLevelKey,
                out var abilityToNextLevel,
                out error,
                out failureQuality) ||
            !TryReadCharacterUInt32(
                reader,
                TotalAbilityKey,
                out var totalAbility,
                out error,
                out failureQuality) ||
            !TryReadCharacterUInt32(
                reader,
                WeightKey,
                out var weight,
                out error,
                out failureQuality) ||
            !TryReadCharacterUInt32(
                reader,
                MaximumWeightKey,
                out var maximumWeight,
                out error,
                out failureQuality) ||
            !TryReadCharacterSByte(
                reader,
                ArmorClassKey,
                out var armorClass,
                out error,
                out failureQuality) ||
            !TryReadCharacterByte(
                reader,
                DamageModifierKey,
                out var damageModifier,
                out error,
                out failureQuality) ||
            !TryReadCharacterByte(
                reader,
                HitModifierKey,
                out var hitModifier,
                out error,
                out failureQuality) ||
            !TryReadCharacterUInt16(
                reader,
                AttackElementKey,
                out var attackElement,
                out error,
                out failureQuality) ||
            !TryReadCharacterUInt16(
                reader,
                DefenseElementKey,
                out var defenseElement,
                out error,
                out failureQuality) ||
            !TryReadCharacterUInt16(
                reader,
                MagicResistanceKey,
                out var magicResistance,
                out error,
                out failureQuality) ||
            !TryReadCharacterByte(
                reader,
                ActionStateKey,
                out var actionState,
                out error,
                out failureQuality) ||
            !TryReadCharacterUInt32(
                reader,
                ShowAbilityMetadataKey,
                out var showAbilityMetadata,
                out error,
                out failureQuality) ||
            !TryReadCharacterUInt32(
                reader,
                ShowMasterMetadataKey,
                out var showMasterMetadata,
                out error,
                out failureQuality))
        {
            character = null;
            return false;
        }

        var userStateValue = (byte)(rawUserState & byte.MaxValue);
        var userState = userStateValue <=
            (byte)CharacterUserState.NeedHelp
            ? (CharacterUserState)userStateValue
            : CharacterUserState.Unknown;
        character = new CharacterSnapshot(
            characterClass,
            presence.Level,
            abilityLevel,
            presence.CharacterName,
            characterId,
            userState,
            privilegeLevel,
            gold,
            totalExperience,
            strength,
            dexterity,
            wisdom,
            constitution,
            intelligence,
            statPoints,
            experienceToNextLevel,
            gamePoints,
            abilityToNextLevel,
            totalAbility,
            weight,
            maximumWeight,
            armorClass,
            damageModifier,
            hitModifier,
            attackElement,
            defenseElement,
            magicResistance,
            actionState,
            showAbilityMetadata != 0,
            showMasterMetadata != 0);
        error = null;
        failureQuality = SnapshotQuality.Unknown;
        return true;
    }

    private static bool TryReadVitals(
        MappedMemoryReader reader,
        out VitalsSnapshot? vitals,
        out SnapshotCaptureError? error,
        out SnapshotQuality failureQuality)
    {
        if (!TryReadUInt32AsInt32(
                reader,
                CurrentHealthKey,
                SnapshotSection.Vitals,
                out var currentHealth,
                out error,
                out failureQuality) ||
            !TryReadUInt32AsInt32(
                reader,
                MaximumHealthKey,
                SnapshotSection.Vitals,
                out var maximumHealth,
                out error,
                out failureQuality) ||
            !TryReadUInt32AsInt32(
                reader,
                CurrentManaKey,
                SnapshotSection.Vitals,
                out var currentMana,
                out error,
                out failureQuality) ||
            !TryReadUInt32AsInt32(
                reader,
                MaximumManaKey,
                SnapshotSection.Vitals,
                out var maximumMana,
                out error,
                out failureQuality))
        {
            vitals = null;
            return false;
        }

        vitals = new VitalsSnapshot(
            currentHealth,
            maximumHealth,
            currentMana,
            maximumMana);
        error = null;
        failureQuality = SnapshotQuality.Unknown;
        return true;
    }

    private static bool TryReadClientState(
        MappedMemoryReader reader,
        out ClientPanel activePanel,
        out bool isInventoryExpanded,
        out bool isUserChatting,
        out SnapshotCaptureError? error)
    {
        if (!reader.TryReadByte(
                ActivePanelKey,
                out var rawPanel,
                out var panelError))
        {
            activePanel = ClientPanel.Unknown;
            isInventoryExpanded = false;
            isUserChatting = false;
            error = MappingFailure(
                SnapshotSection.ClientState,
                ActivePanelKey,
                panelError);
            return false;
        }

        activePanel = rawPanel switch
        {
            0 => ClientPanel.Inventory,
            1 => ClientPanel.TemuairSpells,
            2 => ClientPanel.MedeniaSpells,
            3 => ClientPanel.TemuairSkills,
            4 => ClientPanel.MedeniaSkills,
            5 => ClientPanel.Chat,
            6 => ClientPanel.ChatHistory,
            7 => ClientPanel.Stats,
            8 => ClientPanel.Modifiers,
            9 => ClientPanel.WorldSkills,
            10 => ClientPanel.WorldSpells,
            _ => ClientPanel.Unknown
        };

        if (!reader.TryReadByte(
                InventoryExpandedKey,
                out var rawInventoryExpanded,
                out var inventoryExpandedError))
        {
            isInventoryExpanded = false;
            isUserChatting = false;
            error = MappingFailure(
                SnapshotSection.ClientState,
                InventoryExpandedKey,
                inventoryExpandedError);
            return false;
        }

        isInventoryExpanded = rawInventoryExpanded != 0;
        if (!reader.TryReadByte(
                UserChattingKey,
                out var rawUserChatting,
                out var userChattingError))
        {
            isUserChatting = false;
            error = MappingFailure(
                SnapshotSection.ClientState,
                UserChattingKey,
                userChattingError);
            return false;
        }

        isUserChatting = rawUserChatting != 0;
        error = null;
        return true;
    }

    private static bool TryReadLocation(
        MappedMemoryReader reader,
        out MapLocationSnapshot? location,
        out SnapshotCaptureError? error,
        out SnapshotQuality failureQuality)
    {
        if (!reader.TryReadUInt32(
                MapNumberKey,
                out var mapNumber,
                out var mapNumberError))
        {
            location = null;
            error = MappingFailure(
                SnapshotSection.Location,
                MapNumberKey,
                mapNumberError);
            failureQuality = SnapshotQuality.Partial;
            return false;
        }

        if (mapNumber == 0 || mapNumber > int.MaxValue)
        {
            location = null;
            error = InvalidValue(
                SnapshotSection.Location,
                MapNumberKey,
                $"Map number {mapNumber} is outside the supported range.");
            failureQuality = SnapshotQuality.Incoherent;
            return false;
        }

        if (!reader.TryReadInt32(MapXKey, out var x, out var xError))
        {
            location = null;
            error = MappingFailure(
                SnapshotSection.Location,
                MapXKey,
                xError);
            failureQuality = SnapshotQuality.Partial;
            return false;
        }

        if (x < 0)
        {
            location = null;
            error = InvalidValue(
                SnapshotSection.Location,
                MapXKey,
                $"Map X coordinate {x} cannot be negative.");
            failureQuality = SnapshotQuality.Incoherent;
            return false;
        }

        if (!reader.TryReadInt32(MapYKey, out var y, out var yError))
        {
            location = null;
            error = MappingFailure(
                SnapshotSection.Location,
                MapYKey,
                yError);
            failureQuality = SnapshotQuality.Partial;
            return false;
        }

        if (y < 0)
        {
            location = null;
            error = InvalidValue(
                SnapshotSection.Location,
                MapYKey,
                $"Map Y coordinate {y} cannot be negative.");
            failureQuality = SnapshotQuality.Incoherent;
            return false;
        }

        if (!reader.TryReadInt32(
                MapWidthKey,
                out var width,
                out var widthError))
        {
            location = null;
            error = MappingFailure(
                SnapshotSection.Location,
                MapWidthKey,
                widthError);
            failureQuality = SnapshotQuality.Partial;
            return false;
        }

        if (!reader.TryReadInt32(
                MapHeightKey,
                out var height,
                out var heightError))
        {
            location = null;
            error = MappingFailure(
                SnapshotSection.Location,
                MapHeightKey,
                heightError);
            failureQuality = SnapshotQuality.Partial;
            return false;
        }

        if (width <= 0 ||
            height <= 0 ||
            x >= width ||
            y >= height)
        {
            location = null;
            error = InvalidValue(
                SnapshotSection.Location,
                MapWidthKey,
                $"Map dimensions {width} by {height} do not contain position ({x}, {y}).");
            failureQuality = SnapshotQuality.Incoherent;
            return false;
        }

        if (!reader.TryReadUInt32(
                MapFlagsKey,
                out var flags,
                out var flagsError))
        {
            location = null;
            error = MappingFailure(
                SnapshotSection.Location,
                MapFlagsKey,
                flagsError);
            failureQuality = SnapshotQuality.Partial;
            return false;
        }

        if (!reader.TryReadByte(
                MapWeatherKey,
                out var weather,
                out var weatherError))
        {
            location = null;
            error = MappingFailure(
                SnapshotSection.Location,
                MapWeatherKey,
                weatherError);
            failureQuality = SnapshotQuality.Partial;
            return false;
        }

        if (!reader.TryReadByte(
                MapTransferActiveKey,
                out var transferActive,
                out var transferError))
        {
            location = null;
            error = MappingFailure(
                SnapshotSection.Location,
                MapTransferActiveKey,
                transferError);
            failureQuality = SnapshotQuality.Partial;
            return false;
        }

        if (transferActive != 0)
        {
            location = null;
            error = StateChanged(
                SnapshotSection.Location,
                MapTransferActiveKey,
                "The client map transfer is still active.");
            failureQuality = SnapshotQuality.Incoherent;
            return false;
        }

        if (!reader.TryReadText(
                MapNameKey,
                StrictAscii,
                out var mapName,
                out var mapNameError,
                requireTerminator: true))
        {
            location = null;
            error = MappingFailure(
                SnapshotSection.Location,
                MapNameKey,
                mapNameError);
            failureQuality = SnapshotQuality.Partial;
            return false;
        }

        if (string.IsNullOrWhiteSpace(mapName))
        {
            location = null;
            error = InvalidValue(
                SnapshotSection.Location,
                MapNameKey,
                "The observed map name is empty.");
            failureQuality = SnapshotQuality.Incoherent;
            return false;
        }

        location = new MapLocationSnapshot(
            (int)mapNumber,
            mapName,
            x,
            y,
            width,
            height,
            flags,
            weather,
            isTransferActive: false);
        error = null;
        failureQuality = SnapshotQuality.Unknown;
        return true;
    }

    private static bool TryValidateCoherence(
        MappedMemoryReader reader,
        MemoryAddress expectedSessionAddress,
        uint expectedCharacterId,
        ClientPanel expectedActivePanel,
        bool expectedInventoryExpanded,
        bool expectedUserChatting,
        MapLocationSnapshot expectedLocation,
        out SnapshotCaptureError? error,
        out SnapshotQuality failureQuality)
    {
        if (!TryReadClientState(
                reader,
                out var activePanel,
                out var isInventoryExpanded,
                out var isUserChatting,
                out var clientStateError))
        {
            error = ReassignSection(
                clientStateError!,
                SnapshotSection.Coherence);
            failureQuality = SnapshotQuality.Partial;
            return false;
        }

        if (isInventoryExpanded != expectedInventoryExpanded)
        {
            error = StateChanged(
                InventoryExpandedKey,
                "The inventory display mode changed during snapshot capture.");
            failureQuality = SnapshotQuality.Incoherent;
            return false;
        }

        if (isUserChatting != expectedUserChatting)
        {
            error = StateChanged(
                UserChattingKey,
                "The user chatting state changed during snapshot capture.");
            failureQuality = SnapshotQuality.Incoherent;
            return false;
        }

        if (activePanel != expectedActivePanel)
        {
            error = StateChanged(
                ActivePanelKey,
                "The active client panel changed during snapshot capture.");
            failureQuality = SnapshotQuality.Incoherent;
            return false;
        }

        if (!TryReadLocation(
                reader,
                out var location,
                out var locationError,
                out failureQuality))
        {
            error = ReassignSection(
                locationError!,
                SnapshotSection.Coherence);
            return false;
        }

        if (location != expectedLocation)
        {
            error = StateChanged(
                MapNumberKey,
                "The client map location changed during snapshot capture.");
            failureQuality = SnapshotQuality.Incoherent;
            return false;
        }

        if (!reader.TryResolveAddress(
                WorldUserFuncKey,
                out var sessionAddress,
                out var sessionError))
        {
            if (IsNullPointer(sessionError))
            {
                error = OwnershipChanged(
                    WorldUserFuncKey,
                    "The client session disappeared during snapshot capture.");
                failureQuality = SnapshotQuality.Incoherent;
                return false;
            }

            error = MappingFailure(
                SnapshotSection.Coherence,
                WorldUserFuncKey,
                sessionError);
            failureQuality = SnapshotQuality.Partial;
            return false;
        }

        if (sessionAddress != expectedSessionAddress)
        {
            error = OwnershipChanged(
                WorldUserFuncKey,
                "The client session root changed during snapshot capture.");
            failureQuality = SnapshotQuality.Incoherent;
            return false;
        }

        if (!reader.TryReadUInt32(
                CharacterIdKey,
                out var characterId,
                out var characterIdError))
        {
            error = MappingFailure(
                SnapshotSection.Coherence,
                CharacterIdKey,
                characterIdError);
            failureQuality = SnapshotQuality.Partial;
            return false;
        }

        if (characterId != expectedCharacterId)
        {
            error = OwnershipChanged(
                CharacterIdKey,
                "The observed character changed during snapshot capture.");
            failureQuality = SnapshotQuality.Incoherent;
            return false;
        }

        error = null;
        failureQuality = SnapshotQuality.Unknown;
        return true;
    }

    private static bool TryReadUInt32AsInt32(
        MappedMemoryReader reader,
        string key,
        SnapshotSection section,
        out int value,
        out SnapshotCaptureError? error,
        out SnapshotQuality failureQuality)
    {
        if (!reader.TryReadUInt32(key, out var rawValue, out var readError))
        {
            value = default;
            error = MappingFailure(section, key, readError);
            failureQuality = SnapshotQuality.Partial;
            return false;
        }

        if (rawValue > int.MaxValue)
        {
            value = default;
            error = InvalidValue(
                section,
                key,
                $"Value {rawValue} exceeds the runtime integer range.");
            failureQuality = SnapshotQuality.Incoherent;
            return false;
        }

        value = (int)rawValue;
        error = null;
        failureQuality = SnapshotQuality.Unknown;
        return true;
    }

    private static bool TryReadCharacterUInt32(
        MappedMemoryReader reader,
        string key,
        out uint value,
        out SnapshotCaptureError? error,
        out SnapshotQuality failureQuality)
    {
        if (reader.TryReadUInt32(key, out value, out var readError))
        {
            error = null;
            failureQuality = SnapshotQuality.Unknown;
            return true;
        }

        error = MappingFailure(
            SnapshotSection.Character,
            key,
            readError);
        failureQuality = SnapshotQuality.Partial;
        return false;
    }

    private static bool TryReadCharacterInt32(
        MappedMemoryReader reader,
        string key,
        out int value,
        out SnapshotCaptureError? error,
        out SnapshotQuality failureQuality)
    {
        if (reader.TryReadInt32(key, out value, out var readError))
        {
            error = null;
            failureQuality = SnapshotQuality.Unknown;
            return true;
        }

        error = MappingFailure(
            SnapshotSection.Character,
            key,
            readError);
        failureQuality = SnapshotQuality.Partial;
        return false;
    }

    private static bool TryReadCharacterUInt16(
        MappedMemoryReader reader,
        string key,
        out ushort value,
        out SnapshotCaptureError? error,
        out SnapshotQuality failureQuality)
    {
        if (reader.TryReadUInt16(key, out value, out var readError))
        {
            error = null;
            failureQuality = SnapshotQuality.Unknown;
            return true;
        }

        error = MappingFailure(
            SnapshotSection.Character,
            key,
            readError);
        failureQuality = SnapshotQuality.Partial;
        return false;
    }

    private static bool TryReadCharacterByte(
        MappedMemoryReader reader,
        string key,
        out byte value,
        out SnapshotCaptureError? error,
        out SnapshotQuality failureQuality)
    {
        if (reader.TryReadByte(key, out value, out var readError))
        {
            error = null;
            failureQuality = SnapshotQuality.Unknown;
            return true;
        }

        error = MappingFailure(
            SnapshotSection.Character,
            key,
            readError);
        failureQuality = SnapshotQuality.Partial;
        return false;
    }

    private static bool TryReadCharacterSByte(
        MappedMemoryReader reader,
        string key,
        out sbyte value,
        out SnapshotCaptureError? error,
        out SnapshotQuality failureQuality)
    {
        if (reader.TryReadSByte(key, out value, out var readError))
        {
            error = null;
            failureQuality = SnapshotQuality.Unknown;
            return true;
        }

        error = MappingFailure(
            SnapshotSection.Character,
            key,
            readError);
        failureQuality = SnapshotQuality.Partial;
        return false;
    }

    private static bool TryMapCharacterClass(
        byte rawValue,
        out CharacterClass characterClass)
    {
        // Client memory uses sequential identifiers. PlayerClass bit flags are
        // metadata filters and must not be used to decode this field.
        characterClass = rawValue switch
        {
            0x00 => CharacterClass.Peasant,
            0x01 => CharacterClass.Warrior,
            0x02 => CharacterClass.Rogue,
            0x03 => CharacterClass.Wizard,
            0x04 => CharacterClass.Priest,
            0x05 => CharacterClass.Monk,
            _ => CharacterClass.Unknown
        };
        return characterClass != CharacterClass.Unknown;
    }

    private static bool IsValidCharacterName(string name)
    {
        if (name.Length is < 1 or > 12 || !IsAsciiLetter(name[0]))
        {
            return false;
        }

        return name.All(
            character =>
                IsAsciiLetter(character) ||
                character is >= '0' and <= '9' ||
                character == '-');
    }

    private static bool IsAsciiLetter(char character) =>
        character is >= 'A' and <= 'Z' or >= 'a' and <= 'z';

    private static bool IsNullPointer(MappedMemoryReadError? error) =>
        error is
        {
            Failure: MappedMemoryReadFailure.AddressResolutionFailed,
            MemoryError.Failure: MemoryReadFailure.NullPointer
        };

    private static SnapshotCaptureError MappingFailure(
        SnapshotSection section,
        string key,
        MappedMemoryReadError? error) =>
        new(
            section,
            SnapshotCaptureFailure.MappingReadFailed,
            $"Unable to read mapped value '{key}'.",
            key,
            error);

    private static SnapshotCaptureError InvalidValue(
        SnapshotSection section,
        string key,
        string message) =>
        new(
            section,
            SnapshotCaptureFailure.InvalidValue,
            message,
            key);

    private static SnapshotCaptureError OwnershipChanged(
        string key,
        string message) =>
        new(
            SnapshotSection.Coherence,
            SnapshotCaptureFailure.OwnershipChanged,
            message,
            key);

    private static SnapshotCaptureError StateChanged(
        string key,
        string message) =>
        StateChanged(SnapshotSection.Coherence, key, message);

    private static SnapshotCaptureError StateChanged(
        SnapshotSection section,
        string key,
        string message) =>
        new(
            section,
            SnapshotCaptureFailure.StateChanged,
            message,
            key);

    private static SnapshotCaptureError ReassignSection(
        SnapshotCaptureError error,
        SnapshotSection section) =>
        new(
            section,
            error.Failure,
            error.Message,
            error.VariableKey,
            error.ReadError);

    private static void AddSection(
        ImmutableArray<SnapshotSectionMetrics>.Builder sections,
        SnapshotSection section,
        MacroTimestamp startedAt,
        MacroTimestamp completedAt,
        MemoryReadMetrics readsBefore,
        MemoryReadMetrics readsAfter,
        bool succeeded)
    {
        sections.Add(
            new SnapshotSectionMetrics(
                section,
                completedAt.Elapsed - startedAt.Elapsed,
                succeeded,
                Difference(readsBefore, readsAfter)));
    }

    private static MemoryReadMetrics Difference(
        MemoryReadMetrics before,
        MemoryReadMetrics after) =>
        new(
            after.RequestCount - before.RequestCount,
            after.TransportReadCount - before.TransportReadCount,
            after.FailedReadCount - before.FailedReadCount,
            after.RequestedBytes - before.RequestedBytes,
            after.BytesRead - before.BytesRead);

    private static MemoryReadMetrics EmptyReadMetrics() =>
        new(
            RequestCount: 0,
            TransportReadCount: 0,
            FailedReadCount: 0,
            RequestedBytes: 0,
            BytesRead: 0);

    private static void ValidateSchema(ClientMemoryMap map)
    {
        foreach (var required in RequiredVariables)
        {
            var variable = map.Find(required.Key);
            if (variable is null)
            {
                throw new ArgumentException(
                    $"The client mapping is missing required variable '{required.Key}'.",
                    nameof(map));
            }

            if (variable.ValueKind != required.ValueKind)
            {
                throw new ArgumentException(
                    $"Client mapping variable '{required.Key}' must have value kind '{required.ValueKind}'.",
                    nameof(map));
            }

            if (variable.RequiresSearch)
            {
                throw new ArgumentException(
                    $"Client mapping variable '{required.Key}' cannot require address search.",
                nameof(map));
            }
        }

        ValidateBinaryLayout(
            map,
            InventoryKey,
            maximumLength: ClientInventoryParser.NameLength,
            recordSize: ClientInventoryParser.RecordSize,
            capacity: ClientInventoryParser.RecordCount);
        ValidateBinaryLayout(
            map,
            InventoryPanesKey,
            maximumLength: 0,
            recordSize: ClientInventoryParser.PanePointerSize,
            capacity: ClientInventoryParser.RecordCount);
        ValidateBinaryLayout(
            map,
            EquipmentKey,
            maximumLength: ClientEquipmentParser.CompactNameLength,
            recordSize: ClientEquipmentParser.CompactNameLength,
            capacity: ClientEquipmentParser.RecordCount);
        ValidateBinaryLayout(
            map,
            EquipmentSnapshotKey,
            maximumLength: 0,
            recordSize: ClientEquipmentParser.RichSnapshotSize,
            capacity: ClientEquipmentParser.RecordCount);
        ValidateBinaryLayout(
            map,
            SkillbookKey,
            maximumLength: ClientAbilityParser.NameLength,
            recordSize: ClientAbilityParser.CompactSkillRecordSize,
            capacity: ClientAbilityParser.CompactRecordCount);
        ValidateBinaryLayout(
            map,
            SpellbookKey,
            maximumLength: ClientAbilityParser.NameLength,
            recordSize: ClientAbilityParser.CompactSpellRecordSize,
            capacity: ClientAbilityParser.CompactRecordCount);
        ValidateBinaryLayout(
            map,
            SkillbookPanesKey,
            maximumLength: 0,
            recordSize: ClientAbilityParser.PanePointerSize,
            capacity: ClientAbilityParser.PaneRecordCount);
        ValidateBinaryLayout(
            map,
            SpellbookPanesKey,
            maximumLength: 0,
            recordSize: ClientAbilityParser.PanePointerSize,
            capacity: ClientAbilityParser.PaneRecordCount);
        ValidateBinaryLayout(
            map,
            GroupMemberCacheKey,
            maximumLength: ClientGroupParser.NameLength,
            recordSize: ClientGroupParser.RecordSize,
            capacity: ClientGroupParser.RecordCount);
        ValidateBinaryLayout(
            map,
            ActiveSpellEffectsKey,
            maximumLength: 0,
            recordSize: ClientSpellEffectParser.SnapshotSize,
            capacity: ClientSpellEffectParser.RecordCount);
    }

    private static void ValidateBinaryLayout(
        ClientMemoryMap map,
        string key,
        int maximumLength,
        int recordSize,
        int capacity)
    {
        var variable = map.Find(key)!;
        if (variable.MaximumLength != maximumLength ||
            variable.RecordSize != recordSize ||
            variable.Capacity != capacity)
        {
            throw new ArgumentException(
                $"Client mapping variable '{key}' does not match the supported binary layout.",
                nameof(map));
        }
    }

    private readonly record struct RequiredVariable(
        string Key,
        MemoryValueKind ValueKind);

    private readonly record struct PresenceObservation(
        ClientPresence Presence,
        MemoryAddress SessionAddress,
        string CharacterName,
        byte Level)
    {
        public static PresenceObservation LoggedOut { get; } = new(
            ClientPresence.LoggedOut,
            MemoryAddress.Null,
            string.Empty,
            Level: 0);
    }

    private readonly record struct LocationIdentity(
        int MapNumber,
        string MapName);
}
