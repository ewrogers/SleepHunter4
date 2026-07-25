using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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
    public const string CurrentFileExtension = ".sh4x";
    public const string LegacyFileExtension = ".sh4";

    private const string CurrentFormat =
        "SleepHunter.MacroConfiguration";
    private const int MaximumDocumentBytes = 4 * 1024 * 1024;
    private const int MaximumDocumentCharacters = 4 * 1024 * 1024;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        AllowTrailingCommas = false,
        MaxDepth = 64,
        PropertyNameCaseInsensitive = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Disallow,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter(
                namingPolicy: null,
                allowIntegerValues: false)
        }
    };

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

        try
        {
            var document = ReadBounded(stream);
            return LoadDocument(document);
        }
        finally
        {
            if (!leaveOpen)
                stream.Dispose();
        }
    }

    public static MacroConfigurationLoadResult Load(TextReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var document = ReadBounded(reader);
        return LoadDocument(document);
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
                File.Replace(
                    temporaryPath,
                    fullPath,
                    destinationBackupFileName: null);
            }
            else
            {
                File.Move(temporaryPath, fullPath);
            }
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }

    public static void Save(
        MacroConfiguration configuration,
        Stream stream,
        bool leaveOpen = true)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(stream);

        try
        {
            var document = Serialize(configuration);
            stream.Write(document);
        }
        finally
        {
            if (!leaveOpen)
                stream.Dispose();
        }
    }

    public static void Save(
        MacroConfiguration configuration,
        TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(writer);

        writer.Write(
            Encoding.UTF8.GetString(
                Serialize(configuration)));
    }

    private static MacroConfigurationLoadResult LoadDocument(
        ReadOnlyMemory<byte> document)
    {
        var first = FindFirstContentByte(document.Span);
        return first switch
        {
            (byte)'{' => ReadCurrent(RemoveUtf8Bom(document.Span)),
            (byte)'<' => ReadLegacy(document.Span),
            _ => throw new MacroConfigurationException(
                "Macro configurations must be .sh4x JSON or legacy .sh4 XML.")
        };
    }

    private static MacroConfigurationLoadResult LoadDocument(
        string document)
    {
        var first = document.FirstOrDefault(
            character => !char.IsWhiteSpace(character) &&
                         character != '\uFEFF');
        return first switch
        {
            '{' => ReadCurrent(
                document.Length > 0 &&
                document[0] == '\uFEFF'
                    ? document[1..]
                    : document),
            '<' => ReadLegacy(document),
            _ => throw new MacroConfigurationException(
                "Macro configurations must be .sh4x JSON or legacy .sh4 XML.")
        };
    }

    private static MacroConfigurationLoadResult ReadCurrent(
        ReadOnlySpan<byte> document)
    {
        try
        {
            var serialized = JsonSerializer.Deserialize<CurrentDocument>(
                document,
                JsonOptions);
            return CreateLoadResult(serialized);
        }
        catch (MacroConfigurationException)
        {
            throw;
        }
        catch (Exception exception)
            when (exception is JsonException or
                  ArgumentException or
                  OverflowException)
        {
            throw new MacroConfigurationException(
                $"Unable to parse the .sh4x macro configuration: {exception.Message}",
                exception);
        }
    }

    private static MacroConfigurationLoadResult ReadCurrent(
        string document)
    {
        try
        {
            var serialized = JsonSerializer.Deserialize<CurrentDocument>(
                document,
                JsonOptions);
            return CreateLoadResult(serialized);
        }
        catch (MacroConfigurationException)
        {
            throw;
        }
        catch (Exception exception)
            when (exception is JsonException or
                  ArgumentException or
                  OverflowException)
        {
            throw new MacroConfigurationException(
                $"Unable to parse the .sh4x macro configuration: {exception.Message}",
                exception);
        }
    }

    private static MacroConfigurationLoadResult CreateLoadResult(
        CurrentDocument? serialized)
    {
        if (serialized is null)
        {
            throw new MacroConfigurationException(
                "The .sh4x macro configuration is empty.");
        }

        if (!string.Equals(
                serialized.Format,
                CurrentFormat,
                StringComparison.Ordinal))
        {
            throw new MacroConfigurationException(
                $"Unsupported macro configuration format '{serialized.Format}'.");
        }

        if (!string.Equals(
                serialized.Version,
                CurrentVersion,
                StringComparison.Ordinal))
        {
            throw new MacroConfigurationException(
                $"Unsupported macro configuration version '{serialized.Version}'.");
        }

        if (serialized.Metadata is null ||
            serialized.Skills is null ||
            serialized.Spells is null ||
            serialized.Flowering is null ||
            serialized.Flowering.Queue is null)
        {
            throw new MacroConfigurationException(
                "The .sh4x macro configuration is missing required sections.");
        }

        if (serialized.Skills.Any(entry => entry is null) ||
            serialized.Spells.Any(entry => entry is null) ||
            serialized.Flowering.Queue.Any(entry => entry is null))
        {
            throw new MacroConfigurationException(
                "Macro configuration queues cannot contain null entries.");
        }

        var configuration = new MacroConfiguration(
            serialized.Metadata.Name,
            serialized.Metadata.Description,
            serialized.Metadata.Hotkey is { } hotkey
                ? new HotkeyConfiguration(
                    hotkey.Key,
                    hotkey.Modifiers)
                : null,
            serialized.SpellRotation,
            serialized.Skills
                .Select(
                    entry => new SkillQueueEntry(
                        new SkillQueueEntryId(entry.Id),
                        entry.Name))
                .ToImmutableArray(),
            serialized.Spells
                .Select(CreateSpell)
                .ToImmutableArray(),
            serialized.Flowering.Queue
                .Select(CreateFlower)
                .ToImmutableArray(),
            new FlowerOptions(
                serialized.Flowering.UseVineyard,
                serialized.Flowering.FlowerAlternateCharacters,
                serialized.Flowering.PrioritizeAlternateCharacters,
                serialized.Flowering.MaximumXDistance,
                serialized.Flowering.MaximumYDistance));
        return new MacroConfigurationLoadResult(
            configuration,
            MacroConfigurationFormat.Current,
            CurrentVersion,
            ImmutableArray<MacroConfigurationWarning>.Empty);
    }

    private static SpellQueueEntry CreateSpell(
        SpellDocument entry) =>
        new(
            new SpellQueueEntryId(entry.Id),
            entry.Name,
            entry.TargetLevel,
            CreateTarget(entry.Target),
            new HealthCondition(
                entry.MinimumHealthExclusive,
                entry.MaximumHealthInclusive));

    private static FlowerQueueEntry CreateFlower(
        FlowerDocument entry) =>
        new(
            new FlowerQueueEntryId(entry.Id),
            CreateTarget(entry.Target),
            entry.IntervalTicks is { } ticks
                ? TimeSpan.FromTicks(ticks)
                : null,
            entry.ManaThreshold);

    private static SpellTarget CreateTarget(TargetDocument? target)
    {
        if (target is null)
        {
            throw new MacroConfigurationException(
                "A macro queue entry is missing its target.");
        }

        var offset = new TargetOffset(
            target.OffsetX,
            target.OffsetY);
        return target.Kind switch
        {
            SpellTargetKind.None => SpellTarget.None,
            SpellTargetKind.Self => ApplyOffset(
                SpellTarget.Self,
                offset),
            SpellTargetKind.Character => SpellTarget.Character(
                target.CharacterName ??
                throw new MacroConfigurationException(
                    "A character target is missing its character name."),
                offset),
            SpellTargetKind.RelativeTile => SpellTarget.RelativeTile(
                Required(target.X, "X"),
                Required(target.Y, "Y"),
                offset),
            SpellTargetKind.AbsoluteTile => SpellTarget.AbsoluteTile(
                Required(target.X, "X"),
                Required(target.Y, "Y"),
                offset),
            SpellTargetKind.ScreenPoint => SpellTarget.ScreenPoint(
                Required(target.X, "X"),
                Required(target.Y, "Y"),
                offset),
            SpellTargetKind.RelativeArea => SpellTarget.RelativeArea(
                Required(target.X, "X"),
                Required(target.Y, "Y"),
                Required(target.InnerRadius, "innerRadius"),
                Required(target.OuterRadius, "outerRadius"),
                offset),
            SpellTargetKind.AbsoluteArea => SpellTarget.AbsoluteArea(
                Required(target.X, "X"),
                Required(target.Y, "Y"),
                Required(target.InnerRadius, "innerRadius"),
                Required(target.OuterRadius, "outerRadius"),
                offset),
            _ => throw new MacroConfigurationException(
                $"Unsupported spell target kind '{target.Kind}'.")
        };
    }

    private static SpellTarget ApplyOffset(
        SpellTarget target,
        TargetOffset offset) =>
        offset == TargetOffset.Zero
            ? target
            : target.WithOffset(offset.X, offset.Y);

    private static int Required(int? value, string name) =>
        value ??
        throw new MacroConfigurationException(
            $"A macro target is missing required '{name}' data.");

    private static MacroConfigurationLoadResult ReadLegacy(
        ReadOnlySpan<byte> document)
    {
        using var stream = new MemoryStream(document.ToArray());
        return ReadLegacy(stream);
    }

    private static MacroConfigurationLoadResult ReadLegacy(
        string document)
    {
        using var reader = new StringReader(document);
        var settings = CreateLegacyReaderSettings(closeInput: false);
        using var xmlReader = XmlReader.Create(reader, settings);
        return ReadLegacy(xmlReader);
    }

    private static MacroConfigurationLoadResult ReadLegacy(Stream stream)
    {
        var settings = CreateLegacyReaderSettings(closeInput: false);
        using var reader = XmlReader.Create(stream, settings);
        return ReadLegacy(reader);
    }

    private static MacroConfigurationLoadResult ReadLegacy(
        XmlReader reader)
    {
        try
        {
            var document = XDocument.Load(
                reader,
                LoadOptions.SetLineInfo);
            var root = document.Root ??
                throw new MacroConfigurationException(
                    "The legacy macro configuration has no root element.");
            if (!string.Equals(
                    root.Name.LocalName,
                    "MacroState",
                    StringComparison.Ordinal))
            {
                throw new MacroConfigurationException(
                    $"Unsupported legacy macro root '{root.Name.LocalName}'.");
            }

            return LegacyMacroConfigurationReader.Read(root);
        }
        catch (MacroConfigurationException)
        {
            throw;
        }
        catch (Exception exception)
            when (exception is XmlException or
                  ArgumentException or
                  OverflowException)
        {
            throw new MacroConfigurationException(
                $"Unable to parse the legacy .sh4 macro configuration: {exception.Message}",
                exception);
        }
    }

    private static CurrentDocument CreateDocument(
        MacroConfiguration configuration) =>
        new()
        {
            Format = CurrentFormat,
            Version = CurrentVersion,
            Metadata = new MetadataDocument
            {
                Name = configuration.Name,
                Description = configuration.Description,
                Hotkey = configuration.Hotkey is { } hotkey
                    ? new HotkeyDocument
                    {
                        Key = hotkey.Key,
                        Modifiers = hotkey.Modifiers
                    }
                    : null
            },
            SpellRotation = configuration.SpellRotation,
            Skills = configuration.Skills
                .Select(
                    entry => new SkillDocument
                    {
                        Id = entry.Id.Value,
                        Name = entry.Name
                    })
                .ToArray(),
            Spells = configuration.Spells
                .Select(
                    entry => new SpellDocument
                    {
                        Id = entry.Id.Value,
                        Name = entry.Name,
                        TargetLevel = entry.TargetLevel,
                        MinimumHealthExclusive =
                            entry.HealthCondition
                                .MinimumPercentExclusive,
                        MaximumHealthInclusive =
                            entry.HealthCondition
                                .MaximumPercentInclusive,
                        Target = CreateTarget(entry.Target)
                    })
                .ToArray(),
            Flowering = new FloweringDocument
            {
                UseVineyard =
                    configuration.FlowerOptions.UseVineyard,
                FlowerAlternateCharacters =
                    configuration.FlowerOptions
                        .FlowerAlternateCharacters,
                PrioritizeAlternateCharacters =
                    configuration.FlowerOptions
                        .PrioritizeAlternateCharacters,
                MaximumXDistance =
                    configuration.FlowerOptions.MaximumXDistance,
                MaximumYDistance =
                    configuration.FlowerOptions.MaximumYDistance,
                Queue = configuration.Flowers
                    .Select(
                        entry => new FlowerDocument
                        {
                            Id = entry.Id.Value,
                            IntervalTicks =
                                entry.Interval?.Ticks,
                            ManaThreshold =
                                entry.ManaThreshold,
                            Target =
                                CreateTarget(entry.Target)
                        })
                    .ToArray()
            }
        };

    private static byte[] Serialize(
        MacroConfiguration configuration)
    {
        var document = JsonSerializer.SerializeToUtf8Bytes(
            CreateDocument(configuration),
            JsonOptions);
        if (document.Length > MaximumDocumentBytes)
        {
            throw new MacroConfigurationException(
                $"Macro configurations cannot exceed {MaximumDocumentBytes} bytes.");
        }

        return document;
    }

    private static TargetDocument CreateTarget(
        SpellTarget target) =>
        new()
        {
            Kind = target.Kind,
            CharacterName = target.CharacterName,
            X = target.X,
            Y = target.Y,
            OffsetX = target.Offset.X,
            OffsetY = target.Offset.Y,
            InnerRadius = target.InnerRadius,
            OuterRadius = target.OuterRadius
        };

    private static ReadOnlyMemory<byte> ReadBounded(Stream stream)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        while (true)
        {
            var count = stream.Read(chunk, 0, chunk.Length);
            if (count == 0)
                break;

            if (buffer.Length + count > MaximumDocumentBytes)
            {
                throw new MacroConfigurationException(
                    $"Macro configurations cannot exceed {MaximumDocumentBytes} bytes.");
            }

            buffer.Write(chunk, 0, count);
        }

        return buffer.ToArray();
    }

    private static string ReadBounded(TextReader reader)
    {
        var builder = new StringBuilder();
        var chunk = new char[8192];
        while (true)
        {
            var count = reader.Read(chunk, 0, chunk.Length);
            if (count == 0)
                break;

            if (builder.Length + count > MaximumDocumentCharacters)
            {
                throw new MacroConfigurationException(
                    $"Macro configurations cannot exceed {MaximumDocumentCharacters} characters.");
            }

            builder.Append(chunk, 0, count);
        }

        return builder.ToString();
    }

    private static byte FindFirstContentByte(
        ReadOnlySpan<byte> document)
    {
        var index = document.Length >= 3 &&
                    document[0] == 0xEF &&
                    document[1] == 0xBB &&
                    document[2] == 0xBF
            ? 3
            : 0;
        for (; index < document.Length; index++)
        {
            var value = document[index];
            if (value is not (
                (byte)' ' or
                (byte)'\t' or
                (byte)'\r' or
                (byte)'\n'))
            {
                return value;
            }
        }

        return 0;
    }

    private static ReadOnlySpan<byte> RemoveUtf8Bom(
        ReadOnlySpan<byte> document) =>
        document.Length >= 3 &&
        document[0] == 0xEF &&
        document[1] == 0xBB &&
        document[2] == 0xBF
            ? document[3..]
            : document;

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

    internal static string RequiredAttribute(
        XElement element,
        string name) =>
        element.Attribute(name)?.Value is { Length: > 0 } value
            ? value
            : throw XmlError(
                element,
                $"Required attribute '{name}' is missing.");

    internal static HotkeyModifiers ReadHotkeyModifiers(
        XElement element)
    {
        var value = element.Attribute("Modifiers")?.Value;
        if (value is null)
            return HotkeyModifiers.None;

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

    private static XmlReaderSettings CreateLegacyReaderSettings(
        bool closeInput) =>
        new()
        {
            CloseInput = closeInput,
            DtdProcessing = DtdProcessing.Prohibit,
            IgnoreComments = true,
            IgnoreWhitespace = true,
            MaxCharactersInDocument = MaximumDocumentCharacters,
            XmlResolver = null
        };

    private sealed class CurrentDocument
    {
        public string? Format { get; set; }

        public string? Version { get; set; }

        public MetadataDocument? Metadata { get; set; }

        public SpellQueueRotation? SpellRotation { get; set; }

        public SkillDocument[]? Skills { get; set; }

        public SpellDocument[]? Spells { get; set; }

        public FloweringDocument? Flowering { get; set; }
    }

    private sealed class MetadataDocument
    {
        public string? Name { get; set; }

        public string? Description { get; set; }

        public HotkeyDocument? Hotkey { get; set; }
    }

    private sealed class HotkeyDocument
    {
        public string Key { get; set; } = string.Empty;

        public HotkeyModifiers Modifiers { get; set; }
    }

    private sealed class SkillDocument
    {
        public long Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    private sealed class SpellDocument
    {
        public long Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public int? TargetLevel { get; set; }

        public double? MinimumHealthExclusive { get; set; }

        public double? MaximumHealthInclusive { get; set; }

        public TargetDocument? Target { get; set; }
    }

    private sealed class FloweringDocument
    {
        public bool UseVineyard { get; set; }

        public bool FlowerAlternateCharacters { get; set; }

        public bool PrioritizeAlternateCharacters { get; set; } = true;

        public int MaximumXDistance { get; set; } = 10;

        public int MaximumYDistance { get; set; } = 10;

        public FlowerDocument[]? Queue { get; set; }
    }

    private sealed class FlowerDocument
    {
        public long Id { get; set; }

        public long? IntervalTicks { get; set; }

        public int? ManaThreshold { get; set; }

        public TargetDocument? Target { get; set; }
    }

    private sealed class TargetDocument
    {
        public SpellTargetKind Kind { get; set; }

        public string? CharacterName { get; set; }

        public int? X { get; set; }

        public int? Y { get; set; }

        public int OffsetX { get; set; }

        public int OffsetY { get; set; }

        public int? InnerRadius { get; set; }

        public int? OuterRadius { get; set; }
    }
}
