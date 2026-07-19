using System;
using System.Collections.Generic;
using System.IO;

namespace SleepHunter.Media
{
    internal sealed class ItemPaletteTable
    {
        private readonly Dictionary<int, int> ranges = new();
        private readonly Dictionary<int, int> overrides = new();

        public void Merge(Stream stream)
        {
            using var reader = new StreamReader(stream, leaveOpen: true);

            while (reader.ReadLine() is { } line)
            {
                var values = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                if (values.Length == 2 &&
                    int.TryParse(values[0], out var itemId) &&
                    int.TryParse(values[1], out var paletteId) &&
                    itemId >= 0 && paletteId >= 0)
                {
                    overrides[itemId] = paletteId;
                    continue;
                }

                if (values.Length != 3 ||
                    !int.TryParse(values[0], out var minimum) ||
                    !int.TryParse(values[1], out var maximum) ||
                    !int.TryParse(values[2], out var rangePaletteId) ||
                    minimum < 0 || maximum < minimum || rangePaletteId < 0)
                {
                    continue;
                }

                for (var id = minimum; id <= maximum; id++)
                    ranges[id] = rangePaletteId;
            }
        }

        public int GetPaletteId(int itemId)
        {
            if (overrides.TryGetValue(itemId, out var paletteId))
                return paletteId;

            return ranges.GetValueOrDefault(itemId);
        }
    }
}
