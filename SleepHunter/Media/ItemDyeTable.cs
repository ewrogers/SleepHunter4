using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;

namespace SleepHunter.Media
{
    internal sealed class ItemDyeTable
    {
        public const int StartIndex = 98;
        public const int ColorCount = 6;

        private readonly Dictionary<byte, IReadOnlyList<Color>> entries;

        public static ItemDyeTable Empty { get; } = new(new Dictionary<byte, IReadOnlyList<Color>>());

        private ItemDyeTable(Dictionary<byte, IReadOnlyList<Color>> entries)
        {
            this.entries = entries;
        }

        public static ItemDyeTable Parse(Stream stream)
        {
            using var reader = new StreamReader(stream, leaveOpen: true);
            var entries = new Dictionary<byte, IReadOnlyList<Color>>();

            if (!int.TryParse(reader.ReadLine()?.Trim(), out var colorsPerEntry) || colorsPerEntry <= 0)
                return new ItemDyeTable(entries);

            while (reader.ReadLine() is { } indexLine)
            {
                if (!byte.TryParse(indexLine.Trim(), out var colorIndex))
                    continue;

                var colors = new List<Color>(ColorCount);
                var isValid = true;

                for (var index = 0; index < colorsPerEntry; index++)
                {
                    var line = reader.ReadLine();
                    if (line == null || !TryParseColor(line, out var color))
                    {
                        isValid = false;
                        break;
                    }

                    if (index < ColorCount)
                        colors.Add(color);
                }

                if (!isValid)
                    break;

                if (colors.Count == ColorCount)
                    entries[colorIndex] = colors;
            }

            return new ItemDyeTable(entries);
        }

        public IReadOnlyList<Color> GetColors(byte colorIndex) => entries.GetValueOrDefault(colorIndex);

        private static bool TryParseColor(string text, out Color color)
        {
            color = default;
            var channels = text.Split(',', StringSplitOptions.TrimEntries);
            if (channels.Length != 3 ||
                !byte.TryParse(channels[0], out var red) ||
                !byte.TryParse(channels[1], out var green) ||
                !byte.TryParse(channels[2], out var blue))
            {
                return false;
            }

            color = Color.FromRgb(red, green, blue);
            return true;
        }
    }
}
