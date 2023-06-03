using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace SleepHunter.Metadata
{
    public sealed class ComputedSpellLines
    {
        private readonly ConcurrentDictionary<string, int> spellLines = new(StringComparer.OrdinalIgnoreCase);

        public int SpellCount => spellLines.Count;

        public IEnumerable<KeyValuePair<string, int>> SpellLines => from s in spellLines select s;

        public void SetLines(string spellName, int lines)
        {
            spellName = spellName.Trim();
            spellLines[spellName] = lines;
        }

        public bool ContainsLines(string spellName)
        {
            spellName = spellName.Trim();
            return spellLines.ContainsKey(spellName);
        }

        public int? GetLines(string spellName)
        {
            spellName = spellName.Trim();

            if (!spellLines.TryGetValue(spellName, out var lines))
                return null;

            return lines;
        }

        public bool RemoveLines(string spellName)
        {
            spellName = spellName.Trim();
            return spellLines.TryRemove(spellName, out _);
        }

        public void ClearLines() => spellLines.Clear();
    }
}
