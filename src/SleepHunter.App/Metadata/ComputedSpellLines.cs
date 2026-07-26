using System;
using System.Collections.Concurrent;

namespace SleepHunter.Metadata
{
    public sealed class ComputedSpellLines
    {
        private readonly ConcurrentDictionary<string, int> spellLines = new(StringComparer.OrdinalIgnoreCase);

        public void SetLines(string spellName, int lines)
        {
            spellName = spellName.Trim();
            spellLines[spellName] = lines;
        }

        public int? GetLines(string spellName)
        {
            spellName = spellName.Trim();

            if (!spellLines.TryGetValue(spellName, out var lines))
                return null;

            return lines;
        }

    }
}
