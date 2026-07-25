using System.Collections.Immutable;
using System.Text;
using SleepHunter.Interop.Mappings;
using SleepHunter.Interop.Memory;
using SleepHunter.Runtime.Automation.Panels;
using SleepHunter.Runtime.Characters;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Interop.Snapshots;

public sealed class Usda741SnapshotCapture : IClientSnapshotCapture
{
    public const string SupportedVersion = "USDA 7.41";

    private const string WorldUserFuncKey = "WorldUserFunc";
    private const string CharacterNameKey = "CharacterName";
    private const string CharacterIdKey = "CharacterId";
    private const string CharacterClassKey = "CharacterClass";
    private const string LevelKey = "Level";
    private const string AbilityLevelKey = "AbilityLevel";
    private const string CurrentHealthKey = "CurrentHealth";
    private const string MaximumHealthKey = "MaximumHealth";
    private const string CurrentManaKey = "CurrentMana";
    private const string MaximumManaKey = "MaximumMana";
    private const string ActivePanelKey = "ActivePanel";
    private const string MapNumberKey = "MapNumber";
    private const string MapNameKey = "MapName";
    private const string MapXKey = "MapX";
    private const string MapYKey = "MapY";

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
        new(LevelKey, MemoryValueKind.Byte),
        new(AbilityLevelKey, MemoryValueKind.Byte),
        new(CurrentHealthKey, MemoryValueKind.Unsigned32),
        new(MaximumHealthKey, MemoryValueKind.Unsigned32),
        new(CurrentManaKey, MemoryValueKind.Unsigned32),
        new(MaximumManaKey, MemoryValueKind.Unsigned32),
        new(ActivePanelKey, MemoryValueKind.Byte),
        new(MapNumberKey, MemoryValueKind.Unsigned32),
        new(MapNameKey, MemoryValueKind.Text),
        new(MapXKey, MemoryValueKind.Signed32),
        new(MapYKey, MemoryValueKind.Signed32)
    ];

    private readonly ClientIdentity client;
    private readonly ClientMemoryMap map;
    private readonly IProcessMemorySource source;
    private readonly MemoryReadLimits limits;
    private readonly MacroClock clock;
    private int captureInProgress;

    public Usda741SnapshotCapture(
        ClientIdentity client,
        ClientMemoryMap map,
        IProcessMemorySource source,
        MemoryReadLimits limits,
        MacroClock clock)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(limits);
        ArgumentNullException.ThrowIfNull(clock);

        if (!string.Equals(
                map.VersionKey,
                SupportedVersion,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"This snapshot parser supports only '{SupportedVersion}'.",
                nameof(map));
        }

        if (!string.Equals(
                client.Version,
                map.VersionKey,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "The client identity and memory map versions must match.",
                nameof(client));
        }

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
    }

    public ClientIdentity Client => client;

    public SnapshotCaptureResult Capture(SnapshotSequence sequence)
    {
        if (sequence.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sequence),
                sequence,
                "Snapshot sequences must be positive.");
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
            return CaptureCore(sequence, startedAt);
        }
        finally
        {
            Volatile.Write(ref captureInProgress, 0);
        }
    }

    private SnapshotCaptureResult CaptureCore(
        SnapshotSequence sequence,
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

        sectionStartedAt = clock.GetCurrentTimestamp();
        readsBefore = session.Metrics;
        var coherenceSucceeded = TryValidateCoherence(
            reader,
            presence.SessionAddress,
            character!.CharacterId,
            activePanel,
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
            location);
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
        MapLocationSnapshot? location)
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
            vitals: vitals,
            location: location);
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

        character = new CharacterSnapshot(
            characterClass,
            presence.Level,
            abilityLevel,
            presence.CharacterName,
            characterId);
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
        out SnapshotCaptureError? error)
    {
        if (!reader.TryReadByte(
                ActivePanelKey,
                out var rawPanel,
                out var panelError))
        {
            activePanel = ClientPanel.Unknown;
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
            y);
        error = null;
        failureQuality = SnapshotQuality.Unknown;
        return true;
    }

    private static bool TryValidateCoherence(
        MappedMemoryReader reader,
        MemoryAddress expectedSessionAddress,
        uint expectedCharacterId,
        ClientPanel expectedActivePanel,
        MapLocationSnapshot expectedLocation,
        out SnapshotCaptureError? error,
        out SnapshotQuality failureQuality)
    {
        if (!TryReadClientState(
                reader,
                out var activePanel,
                out var clientStateError))
        {
            error = ReassignSection(
                clientStateError!,
                SnapshotSection.Coherence);
            failureQuality = SnapshotQuality.Partial;
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

    private static bool TryMapCharacterClass(
        byte rawValue,
        out CharacterClass characterClass)
    {
        characterClass = rawValue switch
        {
            0x00 => CharacterClass.Peasant,
            0x01 => CharacterClass.Warrior,
            0x02 => CharacterClass.Wizard,
            0x04 => CharacterClass.Priest,
            0x08 => CharacterClass.Rogue,
            0x10 => CharacterClass.Monk,
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
        new(
            SnapshotSection.Coherence,
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
                    $"Client mapping '{map.VersionKey}' is missing required variable '{required.Key}'.",
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
}
