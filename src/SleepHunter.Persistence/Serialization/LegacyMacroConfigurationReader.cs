using System.Collections.Immutable;
using System.Globalization;
using System.Xml.Linq;
using SleepHunter.Persistence.Configuration;
using SleepHunter.Runtime.Automation.Flowering;
using SleepHunter.Runtime.Automation.Skills;
using SleepHunter.Runtime.Automation.Spells;

namespace SleepHunter.Persistence.Serialization;

internal static class LegacyMacroConfigurationReader
{
    public static MacroConfigurationLoadResult Read(XElement root)
    {
        var warnings =
            ImmutableArray.CreateBuilder<MacroConfigurationWarning>();
        var version = root.Attribute("Version")?.Value;
        if (version is not null &&
            !version.StartsWith("4.", StringComparison.Ordinal))
        {
            throw MacroConfigurationSerializer.XmlError(
                root,
                $"Unsupported legacy macro version '{version}'.");
        }

        if (version is null)
        {
            version = "4.x-unspecified";
            warnings.Add(new MacroConfigurationWarning(
                "legacy-version-missing",
                "The legacy macro has no version marker and was interpreted as a version 4 file."));
        }

        if (root.Element("LocalStorage") is not null)
        {
            warnings.Add(new MacroConfigurationWarning(
                "legacy-local-storage-ignored",
                "Legacy private feature storage was ignored because it is not part of the supported macro configuration."));
        }

        var skills = ReadSkills(root.Element("Skills"), warnings);
        var spells = ReadSpells(root.Element("Spells"), warnings);
        var flowers = ReadFlowers(root.Element("Flowering"), warnings);
        var rotation = ReadRotation(
            root.Element("SpellRotation")?.Value,
            warnings);
        var alternateCharacters =
            ReadElementBool(root, "FlowerAlternateCharacters") ?? false;
        var configuration = new MacroConfiguration(
            root.Element("Name")?.Value,
            root.Element("Description")?.Value,
            ReadHotkey(root.Element("Hotkey")),
            rotation,
            skills,
            spells,
            flowers,
            new FlowerOptions(
                useVineyard:
                    ReadElementBool(root, "UseLyliacVineyard") ?? false,
                flowerAlternateCharacters: alternateCharacters,
                prioritizeAlternateCharacters: true));
        return new MacroConfigurationLoadResult(
            configuration,
            MacroConfigurationFormat.LegacyV4,
            version,
            warnings.ToImmutable());
    }

    private static ImmutableArray<SkillQueueEntry> ReadSkills(
        XElement? element,
        ImmutableArray<MacroConfigurationWarning>.Builder warnings)
    {
        if (element is null)
        {
            return ImmutableArray<SkillQueueEntry>.Empty;
        }

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var entries = ImmutableArray.CreateBuilder<SkillQueueEntry>();
        foreach (var skill in element.Elements("Skill"))
        {
            var name = MacroConfigurationSerializer.RequiredAttribute(
                skill,
                "Name");
            if (!names.Add(name))
            {
                warnings.Add(new MacroConfigurationWarning(
                    "legacy-skill-duplicate",
                    $"Duplicate legacy skill '{name}' was ignored."));
                continue;
            }

            entries.Add(new SkillQueueEntry(
                new SkillQueueEntryId(entries.Count + 1),
                name));
        }

        return entries.ToImmutable();
    }

    private static ImmutableArray<SpellQueueEntry> ReadSpells(
        XElement? element,
        ImmutableArray<MacroConfigurationWarning>.Builder warnings)
    {
        if (element is null)
        {
            return ImmutableArray<SpellQueueEntry>.Empty;
        }

        var entries = ImmutableArray.CreateBuilder<SpellQueueEntry>();
        foreach (var spell in element.Elements("Spell"))
        {
            var name = MacroConfigurationSerializer.RequiredAttribute(
                spell,
                "Name");
            var target = ReadTarget(
                spell,
                warnings,
                $"spell '{name}'");
            if (target is null)
            {
                target = SpellTarget.None;
                warnings.Add(new MacroConfigurationWarning(
                    "legacy-spell-target-cleared",
                    $"Legacy spell '{name}' had an unusable target, so its target was cleared."));
            }

            var savedTargetLevel =
                OptionalLegacyInt(spell, "TargetLevel");
            var targetLevel = savedTargetLevel > 0
                ? savedTargetLevel
                : null;
            entries.Add(new SpellQueueEntry(
                new SpellQueueEntryId(entries.Count + 1),
                name,
                targetLevel,
                target));
        }

        return entries.ToImmutable();
    }

    private static ImmutableArray<FlowerQueueEntry> ReadFlowers(
        XElement? element,
        ImmutableArray<MacroConfigurationWarning>.Builder warnings)
    {
        if (element is null)
        {
            return ImmutableArray<FlowerQueueEntry>.Empty;
        }

        var entries = ImmutableArray.CreateBuilder<FlowerQueueEntry>();
        var sourceIndex = 0;
        foreach (var flower in element.Elements("Flower"))
        {
            sourceIndex++;
            var label = $"flower target {sourceIndex}";
            var target = ReadTarget(flower, warnings, label);
            var hasInterval =
                OptionalLegacyBool(flower, "HasInterval") ?? false;
            var interval = ReadInterval(
                flower,
                hasInterval,
                warnings,
                label);
            var savedManaThreshold =
                OptionalLegacyInt(flower, "IfManaLessThan");
            var manaThreshold = savedManaThreshold > 0
                ? savedManaThreshold
                : null;

            if (target is null ||
                target.Kind == SpellTargetKind.None ||
                interval is null && manaThreshold is null ||
                manaThreshold is not null &&
                target.Kind != SpellTargetKind.Character)
            {
                warnings.Add(new MacroConfigurationWarning(
                    "legacy-flower-skipped",
                    $"Legacy {label} was unusable and was not imported."));
                continue;
            }

            entries.Add(new FlowerQueueEntry(
                new FlowerQueueEntryId(entries.Count + 1),
                target,
                interval,
                manaThreshold));
        }

        return entries.ToImmutable();
    }

    private static SpellTarget? ReadTarget(
        XElement element,
        ImmutableArray<MacroConfigurationWarning>.Builder warnings,
        string label)
    {
        var mode = OptionalLegacyEnum<LegacyTargetMode>(
            element,
            "Mode") ?? LegacyTargetMode.None;
        var x = ReadIntegralCoordinate(element, "X");
        var y = ReadIntegralCoordinate(element, "Y");
        var offset = new TargetOffset(
            ReadIntegralCoordinate(element, "OffsetX"),
            ReadIntegralCoordinate(element, "OffsetY"));

        return mode switch
        {
            LegacyTargetMode.None => SpellTarget.None,
            LegacyTargetMode.Self => ApplyOffset(SpellTarget.Self, offset),
            LegacyTargetMode.Character => ReadCharacterTarget(
                element,
                offset),
            LegacyTargetMode.RelativeTile => SpellTarget.RelativeTile(
                x,
                y,
                offset),
            LegacyTargetMode.AbsoluteTile => SpellTarget.AbsoluteTile(
                x,
                y,
                offset),
            LegacyTargetMode.AbsoluteXY => SpellTarget.ScreenPoint(
                x,
                y,
                offset),
            LegacyTargetMode.RelativeRadius => ReadRadiusTarget(
                absolute: false,
                x,
                y,
                offset,
                element,
                warnings,
                label),
            LegacyTargetMode.AbsoluteRadius => ReadRadiusTarget(
                absolute: true,
                x,
                y,
                offset,
                element,
                warnings,
                label),
            _ => throw MacroConfigurationSerializer.XmlError(
                element,
                $"Unsupported legacy target mode '{mode}'.")
        };
    }

    private static SpellTarget? ReadCharacterTarget(
        XElement element,
        TargetOffset offset)
    {
        var name = element.Attribute("Target")?.Value;
        return string.IsNullOrWhiteSpace(name)
            ? null
            : SpellTarget.Character(name, offset);
    }

    private static SpellTarget ReadRadiusTarget(
        bool absolute,
        int x,
        int y,
        TargetOffset offset,
        XElement element,
        ImmutableArray<MacroConfigurationWarning>.Builder warnings,
        string label)
    {
        var innerRadius =
            OptionalLegacyInt(element, "InnerRadius") ?? 0;
        var outerRadius =
            OptionalLegacyInt(element, "OuterRadius") ?? 0;
        if (outerRadius > SpellTarget.MaximumAreaRadius)
        {
            throw MacroConfigurationSerializer.XmlError(
                element,
                $"Legacy {label} exceeds the supported maximum radius of {SpellTarget.MaximumAreaRadius}.");
        }

        if (innerRadius < 0)
        {
            innerRadius = 0;
            warnings.Add(new MacroConfigurationWarning(
                "legacy-radius-normalized",
                $"Legacy {label} had a negative inner radius, which was normalized to zero."));
        }

        if (outerRadius <= 0 || innerRadius > outerRadius)
        {
            warnings.Add(new MacroConfigurationWarning(
                "legacy-radius-single-point",
                $"Legacy {label} had an empty radius and was imported as its center tile."));
            return absolute
                ? SpellTarget.AbsoluteTile(x, y, offset)
                : SpellTarget.RelativeTile(x, y, offset);
        }

        warnings.Add(new MacroConfigurationWarning(
            "legacy-radius-modernized",
            $"Legacy {label} will use exact circular target geometry."));
        return absolute
            ? SpellTarget.AbsoluteArea(
                x,
                y,
                innerRadius,
                outerRadius,
                offset)
            : SpellTarget.RelativeArea(
                x,
                y,
                innerRadius,
                outerRadius,
                offset);
    }

    private static SpellQueueRotation? ReadRotation(
        string? value,
        ImmutableArray<MacroConfigurationWarning>.Builder warnings)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            string.Equals(value.Trim(), "Default", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (string.Equals(value.Trim(), "None", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add(new MacroConfigurationWarning(
                "legacy-rotation-mapped",
                "Legacy spell rotation 'None' was mapped to priority order."));
            return SpellQueueRotation.Priority;
        }

        if (string.Equals(
                value.Trim(),
                "Singular",
                StringComparison.OrdinalIgnoreCase))
        {
            return SpellQueueRotation.Sequential;
        }

        if (string.Equals(
                value.Trim(),
                "RoundRobin",
                StringComparison.OrdinalIgnoreCase))
        {
            return SpellQueueRotation.RoundRobin;
        }

        throw new MacroConfigurationException(
            $"Unsupported legacy spell rotation '{value}'.");
    }

    private static HotkeyConfiguration? ReadHotkey(XElement? element)
    {
        if (element is null)
        {
            return null;
        }

        return new HotkeyConfiguration(
            MacroConfigurationSerializer.RequiredAttribute(element, "Key"),
            MacroConfigurationSerializer.ReadHotkeyModifiers(element));
    }

    private static TimeSpan? ReadInterval(
        XElement element,
        bool hasInterval,
        ImmutableArray<MacroConfigurationWarning>.Builder warnings,
        string label)
    {
        var seconds = OptionalLegacyDouble(element, "Interval") ?? 0;
        if (seconds < 0)
        {
            throw MacroConfigurationSerializer.XmlError(
                element,
                "Legacy flower intervals cannot be negative.");
        }

        if (!hasInterval && seconds <= 0)
        {
            return null;
        }

        if (!hasInterval)
        {
            warnings.Add(new MacroConfigurationWarning(
                "legacy-interval-recovered",
                $"Legacy {label} contained a saved interval that was recovered despite its incorrect HasInterval marker."));
        }

        try
        {
            return TimeSpan.FromSeconds(seconds);
        }
        catch (OverflowException exception)
        {
            throw new MacroConfigurationException(
                "A legacy flower interval is outside the supported range.",
                exception);
        }
    }

    private static SpellTarget ApplyOffset(
        SpellTarget target,
        TargetOffset offset) =>
        offset == TargetOffset.Zero
            ? target
            : target.WithOffset(offset.X, offset.Y);

    private static int ReadIntegralCoordinate(
        XElement element,
        string name)
    {
        var value = OptionalLegacyDouble(element, name) ?? 0;
        if (value < int.MinValue ||
            value > int.MaxValue ||
            Math.Truncate(value) != value)
        {
            throw MacroConfigurationSerializer.XmlError(
                element,
                $"Legacy attribute '{name}' must be an integer coordinate.");
        }

        return (int)value;
    }

    private static bool? ReadElementBool(XElement root, string name)
    {
        var value = root.Element(name)?.Value;
        if (value is null)
        {
            return null;
        }

        return bool.TryParse(value, out var parsed)
            ? parsed
            : throw MacroConfigurationSerializer.XmlError(
                root.Element(name)!,
                $"Element '{name}' must be true or false.");
    }

    private static int? OptionalLegacyInt(XElement element, string name)
    {
        var value = element.Attribute(name)?.Value;
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
            : throw MacroConfigurationSerializer.XmlError(
                element,
                $"Legacy attribute '{name}' must be an integer.");
    }

    private static double? OptionalLegacyDouble(
        XElement element,
        string name)
    {
        var value = element.Attribute(name)?.Value;
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
            : throw MacroConfigurationSerializer.XmlError(
                element,
                $"Legacy attribute '{name}' must be a finite number.");
    }

    private static bool? OptionalLegacyBool(XElement element, string name)
    {
        var value = element.Attribute(name)?.Value;
        if (value is null)
        {
            return null;
        }

        return bool.TryParse(value, out var parsed)
            ? parsed
            : throw MacroConfigurationSerializer.XmlError(
                element,
                $"Legacy attribute '{name}' must be true or false.");
    }

    private static T? OptionalLegacyEnum<T>(XElement element, string name)
        where T : struct, Enum
    {
        var value = element.Attribute(name)?.Value;
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
            : throw MacroConfigurationSerializer.XmlError(
                element,
                $"Legacy attribute '{name}' has unsupported value '{value}'.");
    }

    private enum LegacyTargetMode
    {
        None,
        Self,
        Character,
        RelativeTile,
        AbsoluteTile,
        AbsoluteXY,
        RelativeRadius,
        AbsoluteRadius
    }
}
