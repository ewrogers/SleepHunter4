using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SleepHunter.Metadata
{
   public sealed class ComputedSpellLines
   {
      ConcurrentDictionary<string, int> spellLines = new ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase);

      public int SpellCount
      {
         get { return spellLines.Count; }
      }

      public IEnumerable<KeyValuePair<string, int>> SpellLines
      {
         get { return from s in spellLines select s; }
      }

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

         int lines;

         if (!spellLines.TryGetValue(spellName, out lines))
            return null;

         return lines;
      }

      public bool RemoveLines(string spellName)
      {
         spellName = spellName.Trim();

         int lines;
         return spellLines.TryRemove(spellName, out lines);
      }

      public void ClearLines()
      {
         spellLines.Clear();
      }
   }
}
