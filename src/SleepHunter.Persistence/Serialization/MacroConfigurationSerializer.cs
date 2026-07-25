using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using SleepHunter.Persistence.Configuration;
using SleepHunter.Runtime.Automation;
using SleepHunter.Runtime.Automation.Flowering;
using SleepHunter.Runtime.Automation.Skills;
using SleepHunter.Runtime.Automation.Spells;

namespace SleepHunter.Persistence.Serialization;

public static class MacroConfigurationSerializer
{
    public const string CurrentVersion = "1";
    public const string CurrentFileExtension = ".shmacro";
    public const string LegacyFileExtension = ".sh4";

    private const long MaximumDocumentCharacters = 4 * 1024 * 1024;

    public static MacroConfigurationLoadResult Load(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        using var stream = File.OpenRead(filePath);
        return Load(stream, leaveOpen: false);
    }

    public static MacroConfigurationLoadResult Load(
        Stream stream,
        bool leaveOpen = true)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var settings = CreateReaderSettings(closeInput: !leaveOpen);
        using var reader = XmlReader.Create(stream, settings);
        return LoadDocument(reader);
    }

    public static MacroConfigurationLoadResult Load(TextReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var settings = CreateReaderSettings(closeInput: false);
        using var xmlReader = XmlReader.Create(reader, settings);
        return LoadDocument(xmlReader);
    }

    public static void Save(
        MacroConfiguration configuration,
        string filePath)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var fullPath = Path.GetFullPath(filePath);
        var directory = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new ArgumentException(
                "The macro configuration path requires a directory.",
                nameof(filePath));
        }

        Directory.CreateDirectory(directory);
        var temporaryPath = Path.Combine(
            directory,
            $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            using (var stream = new FileStream(
                       temporaryPath,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.None))
            {
                Save(configuration, stream, leaveOpen: true);
                stream.Flush(flushToDisk: true);
            }

            if (File.Exists(fullPath))
            {
                File.Replace(temporaryPath, fullPath, destinationBackupFileName: null);
            }
            else
            {
                File.Move(temporaryPath, fullPath);
            }
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    public static void Save(
        MacroConfiguration configuration,
        Stream stream,
        bool leaveOpen = true)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(stream);

        var settings = new XmlWriterSettings
        {
            CloseOutput = !leaveOpen,
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            Indent = true,
            NewLineHandling = NewLineHandling.None
        };
        using var writer = XmlWriter.Create(stream, settings);
        CreateDocument(configuration).Save(writer);
    }

    public static void Save(
        MacroConfiguration configuration,
        TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(writer);

        var settings = new XmlWriterSettings
        {
            CloseOutput = false,
            Indent = true,
            NewLineHandling = NewLineHandling.None
        };
        using var xmlWriter = XmlWriter.Create(writer, settings);
        CreateDocument(configuration).Save(xmlWriter);
    }

    private static MacroConfigurationLoadResult LoadDocument(XmlReader reader)
    {
        try
        {
            var document = XDocument.Load(reader, LoadOptions.SetLineInfo);
            var root = document.Root ??
                throw new MacroConfigurationException(
                    "The macro configuration document has no root element.");

            return root.Name.LocalName switch
            {
                "MacroConfiguration" => ReadCurrent(root),
                "MacroState" => LegacyMacroConfigurationReader.Read(root),
                _ => throw XmlError(
                    root,
                    $"Unsupported macro configuration root '{root.Name.LocalName}'.")
            };
        }
        catch (MacroConfigurationException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is XmlException or
                FormatException or
                OverflowException or
                ArgumentException)
        {
            throw new MacroConfigurationException(
                $"The macro configuration is invalid: {exception.Message}",
                exception);
        }
    }

    private static MacroConfigurationLoadResult ReadCurrent(XElement root)
    {
        var version = RequiredAttribute(root, "Version");
        if (!string.Equals(version, CurrentVersion, StringComparison.Ordinal))
        {
            throw XmlError(
                root,
                $"Unsupported macro configuration version '{version}'.");
        }

        var metadata = root.Element("Metadata");
        var hotkey = ReadHotkey(metadata?.Element("Hotkey"));
        var skills = ReadSkills(root.Element("Skills"));
        var spellsElement = root.Element("Spells");
        var rotation = OptionalEnum<SpellQueueRotation>(
            spellsElement,
            "Rotation");
        var spells = ReadSpells(spellsElement);
        var flowerElement = root.Element("Flowering");
        var flowerOptions = ReadFlowerOptions(flowerElement);
        var flowers = ReadFlowers(flowerElement);
        var configuration = new MacroConfiguration(
            metadata?.Element("Name")?.Value,
            metadata?.Element("Description")?.Value,
            hotkey,
            rotation,
            skills,
            spells,
            flowers,
            flowerOptions);
        return new MacroConfigurationLoadResult(
            configuration,
            MacroConfigurationFormat.Current,
            CurrentVersion,
            ImmutableArray<MacroConfigurationWarning>.Empty);
    }

    private static ImmutableArray<SkillQueueEntry> ReadSkills(
        XElement? element) =>
        element?.Elements("Skill")
            .Select(skill => new SkillQueueEntry(
                new SkillQueueEntryId(RequiredLong(skill, "Id")),
                RequiredAttribute(skill, "Name")))
            .ToImmutableArray() ??
        ImmutableArray<SkillQueueEntry>.Empty;

    private static ImmutableArray<SpellQueueEntry> ReadSpells(
        XElement? element) =>
        element?.Elements("Spell")
            .Select(spell => new SpellQueueEntry(
                new SpellQueueEntryId(RequiredLong(spell, "Id")),
                RequiredAttribute(spell, "Name"),
                OptionalInt(spell, "TargetLevel"),
                ReadTarget(RequiredElement(spell, "Target")),
                new HealthCondition(
                    OptionalDouble(spell, "MinimumHealthExclusive"),
                    OptionalDouble(spell, "MaximumHealthInclusive"))))
            .ToImmutableArray() ??
        ImmutableArray<SpellQueueEntry>.Empty;

    private static ImmutableArray<FlowerQueueEntry> ReadFlowers(
        XElement? element) =>
        element?.Elements("Flower")
            .Select(flower => new FlowerQueueEntry(
                new FlowerQueueEntryId(RequiredLong(flower, "Id")),
                ReadTarget(RequiredElement(flower, "Target")),
                OptionalLong(flower, "IntervalTicks") is { } ticks
                    ? TimeSpan.FromTicks(ticks)
                    : null,
                OptionalInt(flower, "ManaThreshold")))
            .ToImmutableArray() ??
        ImmutableArray<FlowerQueueEntry>.Empty;

    private static FlowerOptions ReadFlowerOptions(XElement? element) =>
        element is null
            ? FlowerOptions.Default
            : new FlowerOptions(
                OptionalBool(element, "UseVineyard") ?? false,
                OptionalBool(element, "FlowerAlternateCharacters") ?? false,
                OptionalBool(element, "PrioritizeAlternateCharacters") ?? true,
                OptionalInt(element, "MaximumXDistance") ?? 10,
                OptionalInt(element, "MaximumYDistance") ?? 10);

    private static HotkeyConfiguration? ReadHotkey(XElement? element) =>
        element is null
            ? null
            : new HotkeyConfiguration(
                RequiredAttribute(element, "Key"),
                ReadHotkeyModifiers(element));

    private static XDocument CreateDocument(
        MacroConfiguration configuration) =>
        new(
            new XElement(
                "MacroConfiguration",
                new XAttribute("Version", CurrentVersion),
                CreateMetadata(configuration),
                new XElement(
                    "Skills",
                    configuration.Skills.Select(entry =>
                        new XElement(
                            "Skill",
                            new XAttribute("Id", entry.Id.Value),
                            new XAttribute("Name", entry.Name)))),
                new XElement(
                    "Spells",
                    Attribute("Rotation", configuration.SpellRotation),
                    configuration.Spells.Select(CreateSpell)),
                CreateFlowering(configuration)));

    private static XElement CreateMetadata(MacroConfiguration configuration) =>
        new(
            "Metadata",
            Element("Name", configuration.Name),
            Element("Description", configuration.Description),
            configuration.Hotkey is { } hotkey
                ? new XElement(
                    "Hotkey",
                    new XAttribute("Key", hotkey.Key),
                    new XAttribute("Modifiers", hotkey.Modifiers))
                : null);

    private static XElement CreateSpell(SpellQueueEntry entry) =>
        new(
            "Spell",
            new XAttribute("Id", entry.Id.Value),
            new XAttribute("Name", entry.Name),
            Attribute("TargetLevel", entry.TargetLevel),
            Attribute(
                "MinimumHealthExclusive",
                entry.HealthCondition.MinimumPercentExclusive),
            Attribute(
                "MaximumHealthInclusive",
                entry.HealthCondition.MaximumPercentInclusive),
            CreateTarget(entry.Target));

    private static XElement CreateFlowering(
        MacroConfiguration configuration) =>
        new(
            "Flowering",
            new XAttribute(
                "UseVineyard",
                configuration.FlowerOptions.UseVineyard),
            new XAttribute(
                "FlowerAlternateCharacters",
                configuration.FlowerOptions.FlowerAlternateCharacters),
            new XAttribute(
                "PrioritizeAlternateCharacters",
                configuration.FlowerOptions.PrioritizeAlternateCharacters),
            new XAttribute(
                "MaximumXDistance",
                configuration.FlowerOptions.MaximumXDistance),
            new XAttribute(
                "MaximumYDistance",
                configuration.FlowerOptions.MaximumYDistance),
            configuration.Flowers.Select(entry =>
                new XElement(
                    "Flower",
                    new XAttribute("Id", entry.Id.Value),
                    Attribute("IntervalTicks", entry.Interval?.Ticks),
                    Attribute("ManaThreshold", entry.ManaThreshold),
                    CreateTarget(entry.Target))));

    internal static XElement CreateTarget(SpellTarget target) =>
        new(
            "Target",
            new XAttribute("Kind", target.Kind),
            Attribute("Character", target.CharacterName),
            Attribute("X", target.X),
            Attribute("Y", target.Y),
            Attribute(
                "OffsetX",
                target.Offset.X == 0 ? null : (int?)target.Offset.X),
            Attribute(
                "OffsetY",
                target.Offset.Y == 0 ? null : (int?)target.Offset.Y),
            Attribute("InnerRadius", target.InnerRadius),
            Attribute("OuterRadius", target.OuterRadius));

    internal static SpellTarget ReadTarget(XElement element)
    {
        var kind = RequiredEnum<SpellTargetKind>(element, "Kind");
        var x = OptionalInt(element, "X");
        var y = OptionalInt(element, "Y");
        var offset = new TargetOffset(
            OptionalInt(element, "OffsetX") ?? 0,
            OptionalInt(element, "OffsetY") ?? 0);
        var target = kind switch
        {
            SpellTargetKind.None => SpellTarget.None,
            SpellTargetKind.Self => SpellTarget.Self,
            SpellTargetKind.Character => SpellTarget.Character(
                RequiredAttribute(element, "Character")),
            SpellTargetKind.RelativeTile => SpellTarget.RelativeTile(
                RequiredCoordinate(element, "X", x),
                RequiredCoordinate(element, "Y", y),
                offset),
            SpellTargetKind.AbsoluteTile => SpellTarget.AbsoluteTile(
                RequiredCoordinate(element, "X", x),
                RequiredCoordinate(element, "Y", y),
                offset),
            SpellTargetKind.ScreenPoint => SpellTarget.ScreenPoint(
                RequiredCoordinate(element, "X", x),
                RequiredCoordinate(element, "Y", y),
                offset),
            SpellTargetKind.RelativeArea => SpellTarget.RelativeArea(
                RequiredCoordinate(element, "X", x),
                RequiredCoordinate(element, "Y", y),
                RequiredInt(element, "InnerRadius"),
                RequiredInt(element, "OuterRadius"),
                offset),
            SpellTargetKind.AbsoluteArea => SpellTarget.AbsoluteArea(
                RequiredCoordinate(element, "X", x),
                RequiredCoordinate(element, "Y", y),
                RequiredInt(element, "InnerRadius"),
                RequiredInt(element, "OuterRadius"),
                offset),
            _ => throw XmlError(
                element,
                $"Unsupported target kind '{kind}'.")
        };

        if (offset != TargetOffset.Zero &&
            kind is SpellTargetKind.Self or SpellTargetKind.Character)
        {
            target = target.WithOffset(offset.X, offset.Y);
        }
        else if (offset != TargetOffset.Zero &&
                 kind == SpellTargetKind.None)
        {
            throw XmlError(
                element,
                $"Target kind '{kind}' does not support pixel offsets.");
        }

        return target;
    }

    internal static MacroConfigurationException XmlError(
        XObject node,
        string message)
    {
        var lineInfo = (IXmlLineInfo)node;
        var location = lineInfo.HasLineInfo()
            ? $" at line {lineInfo.LineNumber}, position {lineInfo.LinePosition}"
            : string.Empty;
        return new MacroConfigurationException($"{message}{location}");
    }

    internal static XElement RequiredElement(
        XElement parent,
        string name) =>
        parent.Element(name) ??
        throw XmlError(parent, $"Required element '{name}' is missing.");

    internal static string RequiredAttribute(
        XElement element,
        string name) =>
        element.Attribute(name)?.Value is { Length: > 0 } value
            ? value
            : throw XmlError(
                element,
                $"Required attribute '{name}' is missing.");

    internal static int RequiredInt(XElement element, string name) =>
        OptionalInt(element, name) ??
        throw XmlError(
            element,
            $"Required integer attribute '{name}' is missing.");

    internal static long RequiredLong(XElement element, string name) =>
        OptionalLong(element, name) ??
        throw XmlError(
            element,
            $"Required integer attribute '{name}' is missing.");

    internal static int? OptionalInt(XElement? element, string name)
    {
        var value = element?.Attribute(name)?.Value;
        if (value is null)
        {
            return null;
        }

        return int.TryParse(
            value,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var parsed)
            ? parsed
            : throw XmlError(
                element!,
                $"Attribute '{name}' must be a 32-bit integer.");
    }

    internal static long? OptionalLong(XElement? element, string name)
    {
        var value = element?.Attribute(name)?.Value;
        if (value is null)
        {
            return null;
        }

        return long.TryParse(
            value,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var parsed)
            ? parsed
            : throw XmlError(
                element!,
                $"Attribute '{name}' must be a 64-bit integer.");
    }

    internal static double? OptionalDouble(XElement? element, string name)
    {
        var value = element?.Attribute(name)?.Value;
        if (value is null)
        {
            return null;
        }

        return double.TryParse(
            value,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out var parsed) &&
            double.IsFinite(parsed)
            ? parsed
            : throw XmlError(
                element!,
                $"Attribute '{name}' must be a finite number.");
    }

    internal static bool? OptionalBool(XElement? element, string name)
    {
        var value = element?.Attribute(name)?.Value;
        if (value is null)
        {
            return null;
        }

        return bool.TryParse(value, out var parsed)
            ? parsed
            : throw XmlError(
                element!,
                $"Attribute '{name}' must be true or false.");
    }

    internal static T RequiredEnum<T>(XElement element, string name)
        where T : struct, Enum =>
        OptionalEnum<T>(element, name) ??
        throw XmlError(
            element,
            $"Required enum attribute '{name}' is missing.");

    internal static T? OptionalEnum<T>(XElement? element, string name)
        where T : struct, Enum
    {
        var value = element?.Attribute(name)?.Value;
        if (value is null)
        {
            return null;
        }

        return Enum.TryParse<T>(
            value,
            ignoreCase: true,
            out var parsed) &&
            Enum.IsDefined(parsed)
            ? parsed
            : throw XmlError(
                element!,
                $"Attribute '{name}' has unsupported value '{value}'.");
    }

    internal static HotkeyModifiers ReadHotkeyModifiers(XElement element)
    {
        var value = element.Attribute("Modifiers")?.Value;
        if (value is null)
        {
            return HotkeyModifiers.None;
        }

        const HotkeyModifiers supported =
            HotkeyModifiers.Alt |
            HotkeyModifiers.Control |
            HotkeyModifiers.Shift |
            HotkeyModifiers.Windows;
        return Enum.TryParse<HotkeyModifiers>(
            value,
            ignoreCase: true,
            out var parsed) &&
            (parsed & ~supported) == 0
            ? parsed
            : throw XmlError(
                element,
                $"Attribute 'Modifiers' has unsupported value '{value}'.");
    }

    private static int RequiredCoordinate(
        XElement element,
        string name,
        int? value) =>
        value ??
        throw XmlError(
            element,
            $"Target coordinate '{name}' is missing.");

    private static XElement? Element(string name, string? value) =>
        value is null
            ? null
            : new XElement(name, value);

    private static XAttribute? Attribute<T>(string name, T? value)
        where T : struct =>
        value is null
            ? null
            : new XAttribute(
                name,
                Convert.ToString(
                    value.Value,
                    CultureInfo.InvariantCulture)!);

    private static XAttribute? Attribute(string name, string? value) =>
        value is null
            ? null
            : new XAttribute(name, value);

    private static XmlReaderSettings CreateReaderSettings(bool closeInput) =>
        new()
        {
            CloseInput = closeInput,
            DtdProcessing = DtdProcessing.Prohibit,
            IgnoreComments = true,
            IgnoreWhitespace = true,
            MaxCharactersInDocument = MaximumDocumentCharacters,
            XmlResolver = null
        };
}
