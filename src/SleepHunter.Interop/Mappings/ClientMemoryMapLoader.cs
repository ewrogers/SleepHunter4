using System.Collections.Immutable;
using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using SleepHunter.Interop.Memory;

namespace SleepHunter.Interop.Mappings;

public static class ClientMemoryMapLoader
{
    public static ClientMemoryMap Load(
        Stream stream,
        ClientMemoryMapLoadLimits? limits = null)
    {
        ArgumentNullException.ThrowIfNull(stream);

        limits ??= ClientMemoryMapLoadLimits.Default;
        var settings = new XmlReaderSettings
        {
            CloseInput = false,
            DtdProcessing = DtdProcessing.Prohibit,
            MaxCharactersFromEntities = 0,
            MaxCharactersInDocument = limits.MaximumCharacters,
            XmlResolver = null
        };

        using var reader = XmlReader.Create(stream, settings);
        var document = XDocument.Load(reader, LoadOptions.SetLineInfo);
        var root = document.Root;
        if (root?.Name != "ClientLayout")
        {
            throw Format(
                root,
                "The mapping document root must be 'ClientLayout'.");
        }

        return ParseLayout(root, limits);
    }

    private static ClientMemoryMap ParseLayout(
        XElement layout,
        ClientMemoryMapLoadLimits limits)
    {
        var pointerWidthValue = RequiredAttribute(layout, "PointerWidth");
        if (!Enum.TryParse<PointerWidth>(
                pointerWidthValue,
                ignoreCase: true,
                out var pointerWidth) ||
            !Enum.IsDefined(pointerWidth))
        {
            throw Format(
                layout,
                $"The client mapping has unsupported pointer width '{pointerWidthValue}'.");
        }

        var variablesElement = layout.Element("Variables");
        if (variablesElement is null)
        {
            throw Format(
                layout,
                "The client mapping has no 'Variables' element.");
        }

        var variableElements = variablesElement.Elements().ToArray();
        if (variableElements.Length == 0)
        {
            throw Format(
                variablesElement,
                "The client mapping has no memory variables.");
        }

        if (variableElements.Length > limits.MaximumVariables)
        {
            throw Format(
                variablesElement,
                $"The client mapping exceeds the {limits.MaximumVariables} variable limit.");
        }

        var variables = variableElements
            .Select(
                variable => ParseVariable(
                    variable,
                    pointerWidth,
                    limits))
            .ToArray();
        return new ClientMemoryMap(pointerWidth, variables);
    }

    private static MemoryVariableDefinition ParseVariable(
        XElement element,
        PointerWidth pointerWidth,
        ClientMemoryMapLoadLimits limits)
    {
        var elementKind = element.Name.LocalName;
        if (elementKind is not ("Static" or "Dynamic" or "Search"))
        {
            throw Format(
                element,
                $"Unsupported memory variable element '{elementKind}'.");
        }

        var key = RequiredAttribute(element, "Key");
        var address = ParseAddress(
            element,
            RequiredAttribute(element, "Address"),
            pointerWidth);
        var maximumLength = OptionalNonNegativeInteger(
            element,
            "MaxLength");
        var recordSize = OptionalNonNegativeInteger(element, "Size");
        var capacity = OptionalNonNegativeInteger(element, "Count");
        var offsets = ParseOffsets(element, elementKind, limits);
        var valueKind = ParseValueKind(
            element,
            maximumLength,
            recordSize,
            capacity);
        var search = elementKind == "Search"
            ? new MemoryAddressSearch(
                ParseSignedHexAttribute(
                    element,
                    "Offset",
                    "IsNegative"))
            : null;

        return new MemoryVariableDefinition(
            key,
            new PointerChain(address, offsets),
            valueKind,
            maximumLength,
            recordSize,
            capacity,
            search);
    }

    private static ImmutableArray<PointerOffset> ParseOffsets(
        XElement variable,
        string elementKind,
        ClientMemoryMapLoadLimits limits)
    {
        var offsetsElement = variable.Element("Offsets");
        if (elementKind == "Static")
        {
            if (offsetsElement is not null)
            {
                throw Format(
                    offsetsElement,
                    "Static memory variables cannot contain pointer offsets.");
            }

            return ImmutableArray<PointerOffset>.Empty;
        }

        if (offsetsElement is null)
        {
            throw Format(
                variable,
                $"{elementKind} memory variable '{RequiredAttribute(variable, "Key")}' has no pointer offsets.");
        }

        var offsetElements = offsetsElement.Elements("Offset").ToArray();
        if (offsetElements.Length == 0)
        {
            throw Format(
                offsetsElement,
                $"{elementKind} memory variable '{RequiredAttribute(variable, "Key")}' has no pointer offsets.");
        }

        if (offsetElements.Length > limits.MaximumOffsetsPerVariable)
        {
            throw Format(
                offsetsElement,
                $"Memory variable '{RequiredAttribute(variable, "Key")}' exceeds the {limits.MaximumOffsetsPerVariable} pointer offset limit.");
        }

        return offsetElements
            .Select(
                offset => ParseSignedHexAttribute(
                    offset,
                    "Value",
                    "IsNegative"))
            .ToImmutableArray();
    }

    private static MemoryValueKind ParseValueKind(
        XElement variable,
        int maximumLength,
        int recordSize,
        int capacity)
    {
        var type = (string?)variable.Attribute("Type");
        if (!string.IsNullOrWhiteSpace(type))
        {
            return type.Trim() switch
            {
                "Byte" => MemoryValueKind.Byte,
                "SByte" => MemoryValueKind.SByte,
                "Int16" => MemoryValueKind.Signed16,
                "UInt16" => MemoryValueKind.Unsigned16,
                "Int32" => MemoryValueKind.Signed32,
                "UInt32" => MemoryValueKind.Unsigned32,
                "Int64" => MemoryValueKind.Signed64,
                "UInt64" => MemoryValueKind.Unsigned64,
                "String" or "Text" => MemoryValueKind.Text,
                "Binary" => MemoryValueKind.Binary,
                _ => throw Format(
                    variable,
                    $"Memory variable '{RequiredAttribute(variable, "Key")}' has unsupported type '{type}'.")
            };
        }

        if (recordSize > 0 || capacity > 0)
        {
            return MemoryValueKind.Binary;
        }

        if (maximumLength > 0)
        {
            return MemoryValueKind.Text;
        }

        throw Format(
            variable,
            $"Memory variable '{RequiredAttribute(variable, "Key")}' must declare a type or a bounded binary or text layout.");
    }

    private static MemoryAddress ParseAddress(
        XElement element,
        string value,
        PointerWidth pointerWidth)
    {
        if (!TryParseHex(value, out var parsed) || parsed == 0)
        {
            throw Format(
                element,
                $"Memory address '{value}' is not a positive hexadecimal address.");
        }

        if (pointerWidth == PointerWidth.Bit32 && parsed > uint.MaxValue)
        {
            throw Format(
                element,
                $"Memory address '{value}' does not fit the 32-bit client mapping.");
        }

        return new MemoryAddress(parsed);
    }

    private static PointerOffset ParseSignedHexAttribute(
        XElement element,
        string valueName,
        string negativeName)
    {
        var rawValue = RequiredAttribute(element, valueName);
        if (!TryParseHex(rawValue, out var magnitude))
        {
            throw Format(
                element,
                $"Pointer offset '{rawValue}' is not hexadecimal.");
        }

        var isNegative = OptionalBoolean(element, negativeName);
        long value;
        if (isNegative)
        {
            if (magnitude > 1UL << 63)
            {
                throw Format(
                    element,
                    $"Negative pointer offset '{rawValue}' does not fit a signed 64-bit value.");
            }

            value = magnitude == 1UL << 63
                ? long.MinValue
                : -(long)magnitude;
        }
        else
        {
            if (magnitude > long.MaxValue)
            {
                throw Format(
                    element,
                    $"Pointer offset '{rawValue}' does not fit a signed 64-bit value.");
            }

            value = (long)magnitude;
        }

        return new PointerOffset(value);
    }

    private static int OptionalNonNegativeInteger(
        XElement element,
        string name)
    {
        var attribute = element.Attribute(name);
        if (attribute is null)
        {
            return 0;
        }

        if (!int.TryParse(
                attribute.Value,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var value) ||
            value < 0)
        {
            throw Format(
                element,
                $"Attribute '{name}' must be a non-negative decimal integer.");
        }

        return value;
    }

    private static bool OptionalBoolean(XElement element, string name)
    {
        var attribute = element.Attribute(name);
        if (attribute is null)
        {
            return false;
        }

        if (!bool.TryParse(attribute.Value, out var value))
        {
            throw Format(
                element,
                $"Attribute '{name}' must be 'true' or 'false'.");
        }

        return value;
    }

    private static string RequiredAttribute(XElement element, string name)
    {
        var value = (string?)element.Attribute(name);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw Format(
                element,
                $"Element '{element.Name.LocalName}' requires attribute '{name}'.");
        }

        return value.Trim();
    }

    private static bool TryParseHex(string value, out ulong parsed)
    {
        var trimmed = value.Trim();
        if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[2..];
        }

        return ulong.TryParse(
            trimmed,
            NumberStyles.AllowHexSpecifier,
            CultureInfo.InvariantCulture,
            out parsed);
    }

    private static InvalidDataException Format(
        XObject? source,
        string message)
    {
        if (source is IXmlLineInfo lineInfo && lineInfo.HasLineInfo())
        {
            message = string.Create(
                CultureInfo.InvariantCulture,
                $"{message} Line {lineInfo.LineNumber}, position {lineInfo.LinePosition}.");
        }

        return new InvalidDataException(message);
    }
}
