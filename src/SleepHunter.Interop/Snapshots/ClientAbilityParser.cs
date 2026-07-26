using System.Buffers.Binary;
using System.Globalization;
using System.Text.RegularExpressions;
using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Interop.Snapshots;

internal static partial class ClientAbilityParser
{
    public const int CompactSkillRecordSize = 0x104;
    public const int CompactSpellRecordSize = 0x206;
    public const int CompactRecordCount = 89;
    public const int PaneRecordCount = 90;
    public const int PanePointerSize = sizeof(uint);
    public const int PaneSnapshotOffset = 0x190;
    public const int SkillPaneSnapshotSize = 0x1B8;
    public const int SpellPaneSnapshotSize = 0x12C;
    public const int NameLength = 256;

    private const int PaneNameLength = 0x80;

    public static SkillbookSnapshot ParseCompactSkills(
        ReadOnlySpan<byte> snapshot,
        int recordCount,
        AbilitySnapshotCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ValidateCompactSnapshot(
            snapshot,
            recordCount,
            CompactSkillRecordSize,
            "skillbook");

        var skills = new List<SkillSnapshot>(recordCount);
        for (var index = 0; index < recordCount; index++)
        {
            var record = snapshot.Slice(
                index * CompactSkillRecordSize,
                CompactSkillRecordSize);
            if (BinaryPrimitives.ReadInt16LittleEndian(record) == 0)
            {
                continue;
            }

            var rawName = ClientText.ReadNullTerminatedAscii(
                record.Slice(4, NameLength));
            var name = ParseName(
                rawName,
                suffixLeft: 0,
                baseNameLength: 0,
                out var currentLevel,
                out var maximumLevel);
            skills.Add(
                CreateSkill(
                    name,
                    index + 1,
                    currentLevel,
                    maximumLevel,
                    isActionDelayed: false,
                    catalog,
                    icon: BinaryPrimitives.ReadUInt16LittleEndian(
                        record.Slice(2, sizeof(ushort)))));
        }

        return CreateSkillbook(skills);
    }

    public static SpellbookSnapshot ParseCompactSpells(
        ReadOnlySpan<byte> snapshot,
        int recordCount,
        AbilitySnapshotCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ValidateCompactSnapshot(
            snapshot,
            recordCount,
            CompactSpellRecordSize,
            "spellbook");

        var spells = new List<SpellSnapshot>(recordCount);
        for (var index = 0; index < recordCount; index++)
        {
            var record = snapshot.Slice(
                index * CompactSpellRecordSize,
                CompactSpellRecordSize);
            if (BinaryPrimitives.ReadInt16LittleEndian(record) == 0)
            {
                continue;
            }

            var rawName = ClientText.ReadNullTerminatedAscii(
                record.Slice(5, NameLength));
            var prompt = ClientText.ReadNullTerminatedAscii(
                record.Slice(0x105, NameLength));
            var name = ParseName(
                rawName,
                suffixLeft: 0,
                baseNameLength: 0,
                out var currentLevel,
                out var maximumLevel);
            spells.Add(
                CreateSpell(
                    name,
                    index + 1,
                    currentLevel,
                    maximumLevel,
                    clientCastLines: 0,
                    isActionDelayed: false,
                    catalog,
                    icon: BinaryPrimitives.ReadUInt16LittleEndian(
                        record.Slice(2, sizeof(ushort))),
                    argumentType: record[4],
                    prompt));
        }

        return CreateSpellbook(spells);
    }

    public static SkillPaneRecord ParseSkillPane(
        ReadOnlySpan<byte> snapshot)
    {
        if (snapshot.Length != SkillPaneSnapshotSize)
        {
            throw new InvalidDataException(
                $"A skill pane snapshot must contain {SkillPaneSnapshotSize} bytes.");
        }

        return new SkillPaneRecord(
            BinaryPrimitives.ReadUInt16LittleEndian(snapshot),
            ClientText.ReadNullTerminatedAscii(
                snapshot.Slice(0x02, PaneNameLength)),
            snapshot[0x182],
            BinaryPrimitives.ReadUInt32LittleEndian(
                snapshot.Slice(0x184, sizeof(uint))),
            BinaryPrimitives.ReadUInt32LittleEndian(
                snapshot.Slice(0x188, sizeof(uint))),
            BinaryPrimitives.ReadUInt32LittleEndian(
                snapshot.Slice(0x18C, sizeof(uint))),
            snapshot[0x190] != 0,
            snapshot[0x192] != 0,
            BinaryPrimitives.ReadInt32LittleEndian(snapshot.Slice(0x1AC, 4)),
            BinaryPrimitives.ReadInt32LittleEndian(snapshot.Slice(0x1B4, 4)));
    }

    public static SpellPaneRecord ParseSpellPane(
        ReadOnlySpan<byte> snapshot)
    {
        if (snapshot.Length != SpellPaneSnapshotSize)
        {
            throw new InvalidDataException(
                $"A spell pane snapshot must contain {SpellPaneSnapshotSize} bytes.");
        }

        return new SpellPaneRecord(
            BinaryPrimitives.ReadUInt16LittleEndian(
                snapshot.Slice(0x02, sizeof(ushort))),
            ClientText.ReadNullTerminatedAscii(
                snapshot.Slice(0x05, PaneNameLength)),
            ClientText.ReadNullTerminatedAscii(
                snapshot.Slice(0x85, PaneNameLength)),
            snapshot[0x00],
            snapshot[0x04],
            snapshot[0x105],
            snapshot[0x107] != 0,
            BinaryPrimitives.ReadInt32LittleEndian(snapshot.Slice(0x120, 4)),
            BinaryPrimitives.ReadInt32LittleEndian(snapshot.Slice(0x128, 4)));
    }

    public static SkillSnapshot CreateSkill(
        SkillPaneRecord record,
        AbilitySnapshotCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        var name = ParseName(
            record.RawName,
            record.NameSuffixLeft,
            record.BaseNameLength,
            out var currentLevel,
            out var maximumLevel);
        return CreateSkill(
            name,
            record.Slot,
            currentLevel,
            maximumLevel,
            record.IsActionDelayed,
            catalog,
            record.Icon,
            record.CooldownProgress,
            record.CooldownStartedAt,
            record.CooldownEndsAt,
            record.IsCooldownVisualActive);
    }

    public static SpellSnapshot CreateSpell(
        SpellPaneRecord record,
        AbilitySnapshotCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        var name = ParseName(
            record.RawName,
            record.NameSuffixLeft,
            record.BaseNameLength,
            out var currentLevel,
            out var maximumLevel);
        return CreateSpell(
            name,
            record.Slot,
            currentLevel,
            maximumLevel,
            record.CastLines,
            record.IsActionDelayed,
            catalog,
            record.Icon,
            record.ArgumentType,
            record.Prompt);
    }

    public static SkillbookSnapshot CreateSkillbook(
        IEnumerable<SkillSnapshot> skills)
    {
        try
        {
            return new SkillbookSnapshot(skills);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidDataException(
                "The client skillbook contains conflicting abilities.",
                exception);
        }
    }

    public static SpellbookSnapshot CreateSpellbook(
        IEnumerable<SpellSnapshot> spells)
    {
        try
        {
            return new SpellbookSnapshot(spells);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidDataException(
                "The client spellbook contains conflicting abilities.",
                exception);
        }
    }

    private static SkillSnapshot CreateSkill(
        string name,
        int slot,
        int currentLevel,
        int maximumLevel,
        bool isActionDelayed,
        AbilitySnapshotCatalog catalog,
        ushort icon = 0,
        uint cooldownProgress = 0,
        uint cooldownStartedAt = 0,
        uint cooldownEndsAt = 0,
        bool isCooldownVisualActive = false)
    {
        var metadata = catalog.FindSkill(name);
        return new SkillSnapshot(
            name,
            slot,
            currentLevel,
            maximumLevel,
            metadata?.ManaCost ?? 0,
            metadata?.Cooldown ?? TimeSpan.Zero,
            metadata?.IsAssail ?? false,
            metadata?.OpensDialog ?? false,
            metadata?.RequiresDisarm ?? false,
            metadata?.HealthCondition,
            isActionDelayed,
            icon,
            cooldownProgress,
            cooldownStartedAt,
            cooldownEndsAt,
            isCooldownVisualActive);
    }

    private static SpellSnapshot CreateSpell(
        string name,
        int slot,
        int currentLevel,
        int maximumLevel,
        int clientCastLines,
        bool isActionDelayed,
        AbilitySnapshotCatalog catalog,
        ushort icon = 0,
        byte argumentType = 0,
        string? prompt = null)
    {
        var metadata = catalog.FindSpell(name);
        var castLines = clientCastLines > 0
            ? clientCastLines
            : metadata?.CastLines > 0
                ? metadata.CastLines
                : 1;
        return new SpellSnapshot(
            name,
            slot,
            currentLevel,
            maximumLevel,
            castLines,
            metadata?.ManaCost ?? 0,
            metadata?.Cooldown ?? TimeSpan.Zero,
            isActionDelayed,
            metadata?.OpensDialog ?? false,
            icon,
            argumentType,
            prompt);
    }

    private static string ParseName(
        string rawName,
        int suffixLeft,
        int baseNameLength,
        out int currentLevel,
        out int maximumLevel)
    {
        var match = AbilityWithLevelRegex().Match(rawName);
        if (match.Success)
        {
            if (!int.TryParse(
                    match.Groups["current"].ValueSpan,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out currentLevel) ||
                !int.TryParse(
                    match.Groups["maximum"].ValueSpan,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out maximumLevel))
            {
                throw new InvalidDataException(
                    "The client ability level exceeds the supported range.");
            }

            return RequireName(match.Groups["name"].Value);
        }

        currentLevel = 0;
        maximumLevel = 0;
        var name = rawName;
        if (baseNameLength > 0 && baseNameLength <= rawName.Length)
        {
            name = rawName[..baseNameLength];
            if (suffixLeft > 0)
            {
                currentLevel = suffixLeft;
            }
        }

        return RequireName(name);
    }

    private static string RequireName(string name)
    {
        var trimmed = name.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new InvalidDataException(
                "A populated client ability has no name.");
        }

        return trimmed;
    }

    private static void ValidateCompactSnapshot(
        ReadOnlySpan<byte> snapshot,
        int recordCount,
        int recordSize,
        string collectionName)
    {
        if (recordCount < 0 || recordCount > CompactRecordCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(recordCount),
                recordCount,
                $"Compact ability record count must be between 0 and {CompactRecordCount}.");
        }

        var expectedLength = checked(recordCount * recordSize);
        if (snapshot.Length != expectedLength)
        {
            throw new InvalidDataException(
                $"A compact {collectionName} snapshot with {recordCount} records must contain {expectedLength} bytes.");
        }
    }

    [GeneratedRegex(
        @"^(?<name>[ a-z0-9'_-]+)\s*\(Lev:(?<current>[0-9]+)/(?<maximum>[0-9]+)\)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AbilityWithLevelRegex();

    internal readonly record struct SkillPaneRecord(
        ushort Icon,
        string RawName,
        byte Slot,
        uint CooldownProgress,
        uint CooldownStartedAt,
        uint CooldownEndsAt,
        bool IsCooldownVisualActive,
        bool IsActionDelayed,
        int NameSuffixLeft,
        int BaseNameLength);

    internal readonly record struct SpellPaneRecord(
        ushort Icon,
        string RawName,
        string Prompt,
        byte Slot,
        byte ArgumentType,
        byte CastLines,
        bool IsActionDelayed,
        int NameSuffixLeft,
        int BaseNameLength);
}
