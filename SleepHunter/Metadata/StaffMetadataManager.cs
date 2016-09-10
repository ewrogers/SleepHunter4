using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace SleepHunter.Metadata
{
    public sealed class StaffMetadataManager
   {
      public static readonly string StaffMetadataFile = "Staves.xml";

      #region Singleton
      static readonly StaffMetadataManager instance = new StaffMetadataManager();

      public static StaffMetadataManager Instance
      {
         get { return instance; }
      }

      private StaffMetadataManager()
      {
         SpellMetadataManager.Instance.SpellAdded += SpellManager_SpellAdded;
         SpellMetadataManager.Instance.SpellChanged += SpellManager_SpellAdded;
      }

      void SpellManager_SpellAdded(object sender, SpellMetadataEventArgs e)
      {
         if (e.Spell != null)
            RecalculateSpellForAllStaves(e.Spell);
      }
      #endregion

      ConcurrentDictionary<string, StaffMetadata> staves = new ConcurrentDictionary<string, StaffMetadata>(StringComparer.OrdinalIgnoreCase);
      ConcurrentDictionary<string, ComputedSpellLines> computedLines = new ConcurrentDictionary<string, ComputedSpellLines>(StringComparer.OrdinalIgnoreCase);

      public event StaffMetadataEventHandler StaffAdded;
      public event StaffMetadataEventHandler StaffUpdated;
      public event StaffMetadataEventHandler StaffRemoved;
      public event SpellLineModifiersEventHandler StaffModifiersChanged;

      #region Collection Properties
      public int Count { get { return staves.Count; } }

      public IEnumerable<StaffMetadata> Staves
      {
         get { return from s in staves.Values orderby s.AbilityLevel, s.Level, s.Name select s; }
      }
      #endregion

      #region Collection Methods
      public void AddStaff(StaffMetadata staff)
      {
         if (staff == null)
            throw new ArgumentNullException("staff");

         var alreadyExists = staves.ContainsKey(staff.Name);

         staves[staff.Name] = staff;
         RecalculateAllSpells(staff);

         if (alreadyExists)
            OnStaffUpdated(staff);
         else
            OnStaffAdded(staff);
      }
      
      public bool ContainsStaff(string staffName)
      {
         staffName = staffName.Trim();
         return staves.ContainsKey(staffName);
      }

      public StaffMetadata GetStaff(string staffName)
      {
         staffName = staffName.Trim();

         StaffMetadata staff;
         if (staves.TryGetValue(staffName, out staff))
            return staff;

         return null;
      }

      public bool RemoveStaff(string staffName)
      {
         StaffMetadata staff;
         ComputedSpellLines lines;

         var wasRemoved = staves.TryRemove(staffName, out staff);
         computedLines.TryRemove(staffName, out lines);

         if (wasRemoved)
            OnStaffRemoved(staff);

         return wasRemoved;
      }

      public bool RenameStaff(string originalName, string newName)
      {
         StaffMetadata staff = null;
         ComputedSpellLines lines = null;

         var wasStaffFound = staves.TryRemove(originalName, out staff);
         var wasLinesFound = computedLines.TryRemove(originalName, out lines);

         if (wasLinesFound)
            computedLines[newName] = lines;

         if (wasStaffFound)
         {
            OnStaffRemoved(staff);
            staves[newName] = staff;
            OnStaffAdded(staff);
         }

         return wasStaffFound;
      }

      public void ClearStaves()
      {
         foreach (var staff in staves.Values)
            OnStaffRemoved(staff);

         staves.Clear();
      }
      #endregion

      #region Spell Lines Methods
      public int? GetLinesWithStaff(string staffName, string spellName)
      {
         if (string.IsNullOrEmpty(staffName))
         {
            var metadata = SpellMetadataManager.Instance.GetSpell(spellName);
            if (metadata == null)
               return null;

            return metadata.NumberOfLines;
         }

         staffName = staffName.Trim();
         spellName = spellName.Trim();
         ComputedSpellLines spellLines = null;

         if (!computedLines.TryGetValue(staffName, out spellLines))
            return null;

         if (!SpellMetadataManager.Instance.ContainsSpell(spellName))
            return null;

         return spellLines.GetLines(spellName);
      }

      public StaffMetadata GetBestStaffForSpell(string spellName, IEnumerable<string> possibleStaves = null, int maximumLevel = 0, int maximumAbilityLevel = 0)
      {
         int? numberOfLines;
         return GetBestStaffForSpell(spellName, out numberOfLines, possibleStaves, maximumLevel, maximumAbilityLevel);
      }

      public StaffMetadata GetBestStaffForSpell(string spellName,  out int? numberOfLines, IEnumerable<string> possibleStaves = null, int maximumLevel = 0, int maximumAbilityLevel = 0)
      {
         numberOfLines = null;

         StaffMetadata bestStaff=null;
         spellName = spellName.Trim();

         var spell = SpellMetadataManager.Instance.GetSpell(spellName);
         int? bestLines = null;

         foreach (var lines in computedLines)
         {
            StaffMetadata staff = null;
            if (!staves.TryGetValue(lines.Key, out staff))
               continue;

            if (possibleStaves != null && !possibleStaves.Contains(staff.Name, StringComparer.OrdinalIgnoreCase))
               continue;

            if (staff.Level > maximumLevel || staff.AbilityLevel > maximumAbilityLevel)
               continue;

            var spellLines = lines.Value.GetLines(spellName);

            if (!spellLines.HasValue)
               continue;

            if (!bestLines.HasValue)
            {
               bestStaff = staff;
               bestLines = spellLines.Value;
            }
            else if (spellLines.Value < bestLines)
            {
               bestStaff = staff;
               bestLines = spellLines.Value;
            }
            else if (spellLines.Value == bestLines)
            {
               if (staff.Level > bestStaff.Level && staff.AbilityLevel > bestStaff.AbilityLevel)
                  bestStaff = staff;
            }
         }

         if (spell != null && bestLines.HasValue && bestLines.Value > spell.NumberOfLines)
         {
            numberOfLines = spell.NumberOfLines;
            bestStaff = StaffMetadata.NoStaff;
         }
         else numberOfLines = bestLines;

         return bestStaff;
      }

      public void RecalculateAllStaves()
      {
         foreach (var staff in staves.Values)
            RecalculateAllSpells(staff);
      }

      public void RecalculateAllSpells(StaffMetadata staff)
      {
         if (staff == null)
            throw new ArgumentNullException("staff");

         var allLines = CalculateLines(staff);
         computedLines[staff.Name] = allLines;
      }

      public void RecalculateSpell(StaffMetadata staff, SpellMetadata spell)
      {
         ComputedSpellLines allLines = null;
         var lines = CalculateLinesForSpell(staff, spell);

         if (!computedLines.TryGetValue(staff.Name, out allLines))
         {
            allLines = CalculateLines(staff);
            computedLines[staff.Name] = allLines;
         }

         allLines.SetLines(spell.Name, lines);
      }

      public void RecalculateSpellForAllStaves(SpellMetadata spell)
      {
         foreach (var staff in staves.Values)
            RecalculateSpell(staff, spell);
      }
      #endregion

      #region File Load & Save Methods
      public void LoadFromFile(string filename)
      {
         using (var inputStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
         {
            LoadFromStream(inputStream);
         }
      }

      public void LoadFromStream(Stream stream)
      {
         var serializer = new XmlSerializer(typeof(StaffMetadataCollection));
         var collection = serializer.Deserialize(stream) as StaffMetadataCollection;

         if (collection != null)
            foreach (var staff in collection.Staves)
               AddStaff(staff);
      }

      public void SaveToFile(string filename)
      {
         using (var outputStream = File.Create(filename))
         {
            SaveToStream(outputStream);
            outputStream.Flush();
         }
      }

      public void SaveToStream(Stream stream)
      {
         var collection = new StaffMetadataCollection(this.Staves);
         var serializer = new XmlSerializer(typeof(StaffMetadataCollection));
         var namespaces = new XmlSerializerNamespaces();
         namespaces.Add("", "");

         serializer.Serialize(stream, collection, namespaces);
      }
      #endregion

      ComputedSpellLines CalculateLines(StaffMetadata staff)
      {
         var staffSpellLines = new ComputedSpellLines();

         foreach (var spell in SpellMetadataManager.Instance.Spells)
         {
            var lines = CalculateLinesForSpell(staff, spell);
            staffSpellLines.SetLines(spell.Name, lines);
         }

         return staffSpellLines;
      }

      int CalculateLinesForSpell(StaffMetadata staff, SpellMetadata spell)
      {
         var originalLines = spell.NumberOfLines;
         var lines = spell.NumberOfLines;

         foreach (var modifiers in staff.Modifiers)
         {
            if (modifiers.MinThreshold > 0 && originalLines < modifiers.MinThreshold)
               continue;

            if (modifiers.MaxThreshold > 0 && originalLines > modifiers.MaxThreshold)
               continue;

            switch (modifiers.Scope)
            {
               case ModifierScope.None:
                  continue;

               case ModifierScope.Single:
                  if (!string.Equals(modifiers.ScopeName, spell.Name, StringComparison.OrdinalIgnoreCase))
                     continue;
                  break;

               case ModifierScope.Group:
                  if (!string.Equals(modifiers.ScopeName, spell.GroupName, StringComparison.OrdinalIgnoreCase))
                     continue;
                  break;
            }

            switch (modifiers.Action)
            {
               case ModifierAction.Increase:
                  lines += modifiers.Value;
                  break;

               case ModifierAction.Decrease:
                  lines -= modifiers.Value;
                  break;

               case ModifierAction.Set:
                  lines = modifiers.Value;
                  break;
            }
         }

         return Math.Max(0, lines);
      }

      #region Event Handler Methods
      void OnStaffAdded(StaffMetadata staff)
      {
         staff.ModifiersAdded += staff_ModifiersChanged;
         staff.ModifiersChanged += staff_ModifiersChanged;
         staff.ModifiersRemoved += staff_ModifiersChanged;

         var handler = this.StaffAdded;

         if (handler != null)
            handler(this, new StaffMetadataEventArgs(staff));
      }

      void OnStaffUpdated(StaffMetadata staff)
      {
         staff.ModifiersAdded += staff_ModifiersChanged;
         staff.ModifiersChanged += staff_ModifiersChanged;
         staff.ModifiersRemoved += staff_ModifiersChanged;

         var handler = this.StaffUpdated;

         if (handler != null)
            handler(this, new StaffMetadataEventArgs(staff));
      }

      void OnStaffRemoved(StaffMetadata staff)
      {
         staff.ModifiersAdded -= staff_ModifiersChanged;
         staff.ModifiersChanged -= staff_ModifiersChanged;
         staff.ModifiersRemoved -= staff_ModifiersChanged;

         var handler = this.StaffRemoved;

         if (handler != null)
            handler(this, new StaffMetadataEventArgs(staff));
      }

      void staff_ModifiersChanged(object sender, SpellLineModifiersEventArgs e)
      {
         var staff = sender as StaffMetadata;
         if (staff == null)
            return;

         OnStaffModifiersChanged(staff, e.Modifiers);
      }

      void OnStaffModifiersChanged(StaffMetadata staff, SpellLineModifiers modifiers)
      {
         RecalculateAllSpells(staff);

         var handler = StaffModifiersChanged;

         if (handler != null)
            handler(staff, new SpellLineModifiersEventArgs(modifiers));
      }
      #endregion
   }
}