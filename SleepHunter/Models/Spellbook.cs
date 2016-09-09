using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using SleepHunter.Common;
using SleepHunter.Extensions;
using SleepHunter.IO.Process;
using SleepHunter.Media;
using SleepHunter.Metadata;

namespace SleepHunter.Models
{
    public sealed class Spellbook : ObservableObject, IEnumerable<Spell>
   {
      static readonly string SpellbookKey = @"Spellbook";

      public static readonly int TemuairSpellCount = 36;
      public static readonly int MedeniaSpellCount = 36;
      public static readonly int WorldSpellCount = 18;

      Player owner;
      List<Spell> spells = new List<Spell>(TemuairSpellCount + MedeniaSpellCount + WorldSpellCount);
      ConcurrentDictionary<string, DateTime> spellCooldownTimestamps = new ConcurrentDictionary<string, DateTime>();
      string activeSpell;

      public Player Owner
      {
         get { return owner; }
         set { SetProperty(ref owner, value); }
      }

      public int Count { get { return spells.Count((spell) => { return !spell.IsEmpty; }); } }

      public IEnumerable<Spell> Spells
      {
         get { return from s in spells select s; }
      }

      public IEnumerable<Spell> TemuairSpells
      {
         get { return from s in spells where s.Panel == InterfacePanel.TemuairSpells && s.Slot < TemuairSpellCount select s; }
      }

      public IEnumerable<Spell> MedeniaSpells
      {
         get { return from s in spells where s.Panel == InterfacePanel.MedeniaSpells && s.Slot < (TemuairSpellCount + MedeniaSpellCount) select s; }
      }

      public IEnumerable<Spell> WorldSpells
      {
         get { return from s in spells where s.Panel == InterfacePanel.WorldSpells && s.Slot < (TemuairSpellCount + MedeniaSpellCount + WorldSpellCount) select s; }
      }

      public string ActiveSpell
      {
         get { return activeSpell; }
         set { SetProperty(ref activeSpell, value); }
      }

      public Spellbook()
         : this(null) { }

      public Spellbook(Player owner)
      {
         this.owner = owner;
         InitialzeSpellbook();
      }

      void InitialzeSpellbook()
      {
         spells.Clear();

         for (int i = 0; i < spells.Capacity; i++)
            spells.Add(Spell.MakeEmpty(i + 1));
      }

      public bool ContainSpell(string spellName)
      {
         spellName = spellName.Trim();

         foreach (var spell in spells)
            if (string.Equals(spell.Name, spellName, StringComparison.OrdinalIgnoreCase))
               return true;

         return false;
      }

      public Spell GetSpell(string spellName)
      {
         spellName = spellName.Trim();

         foreach (var spell in spells)
            if (string.Equals(spell.Name, spellName, StringComparison.OrdinalIgnoreCase))
               return spell;

         return null;
      }

      public bool IsActive(string spellName)
      {
         if (spellName == null)
            return false;

         spellName = spellName.Trim();

         return string.Equals(spellName, activeSpell, StringComparison.OrdinalIgnoreCase);
      }

      public void SetCooldownTimestamp(string spellName, DateTime timestamp)
      {
         spellName = spellName.Trim();
         spellCooldownTimestamps[spellName] = timestamp;
      }

      public bool ClearCooldown(string spellName)
      {
         spellName = spellName.Trim();

         DateTime timestamp;
         return spellCooldownTimestamps.TryRemove(spellName, out timestamp);
      }

      public void ClearAllCooldowns()
      {
         spellCooldownTimestamps.Clear();
      }

      public void Update()
      {
         if (owner == null)
            throw new InvalidOperationException("Player owner is null, cannot update.");

         Update(owner.Accessor);
      }

      public void Update(ProcessMemoryAccessor accessor)
      {
         if (accessor == null)
            throw new ArgumentNullException("accessor");

         var version = Owner.Version;

         if (version == null)
         {
            ResetDefaults();
            return;
         }

         var spellbookVariable = version.GetVariable(SpellbookKey);

         if (spellbookVariable == null)
         {
            ResetDefaults();
            return;
         }
         
         using(var stream = accessor.GetStream())
         using (var reader = new BinaryReader(stream, Encoding.ASCII))
         {
            var spellbookPointer = spellbookVariable.DereferenceValue(reader);

            if (spellbookPointer == 0)
            {
               ResetDefaults();
               return;
            }

            reader.BaseStream.Position = spellbookPointer;
            bool foundFasSpiorad = false;
            bool foundLyliacVineyard = false;
            bool foundLyliacPlant = false;

            for (int i = 0; i < spellbookVariable.Count; i++)
            {
               SpellMetadata metadata = null;

               try
               {
                  bool hasSpell = reader.ReadInt16() != 0;
                  ushort iconIndex = reader.ReadUInt16();
                  SpellTargetMode targetMode = (SpellTargetMode)reader.ReadByte();
                  string name = reader.ReadFixedString(spellbookVariable.MaxLength);
                  string prompt = reader.ReadFixedString(spellbookVariable.MaxLength);
                  reader.ReadByte();

                  int currentLevel, maximumLevel;
                  if (!Ability.TryParseLevels(name, out name, out currentLevel, out maximumLevel))
                  {
                     if (!string.IsNullOrWhiteSpace(name))
                        spells[i].Name = name.Trim();
                  }


                  spells[i].IsEmpty = !hasSpell;
                  spells[i].IconIndex = iconIndex;
                  spells[i].Icon = IconManager.Instance.GetSpellIcon(iconIndex);
                  spells[i].TargetMode = targetMode;
                  spells[i].Name = name;
                  spells[i].Prompt = prompt;
                  spells[i].CurrentLevel = currentLevel;
                  spells[i].MaximumLevel = maximumLevel;

                  if (!spells[i].IsEmpty && !string.IsNullOrWhiteSpace(spells[i].Name))
                     metadata = SpellMetadataManager.Instance.GetSpell(name);

                  spells[i].IsActive = this.IsActive(spells[i].Name);

                  foundFasSpiorad |= string.Equals(spells[i].Name, Spell.FasSpioradKey, StringComparison.OrdinalIgnoreCase);
                  foundLyliacPlant |= string.Equals(spells[i].Name, Spell.LyliacPlantKey, StringComparison.OrdinalIgnoreCase);
                  foundLyliacVineyard |= string.Equals(spells[i].Name, Spell.LyliacVineyardKey, StringComparison.OrdinalIgnoreCase);

                  if (metadata != null)
                  {
                     spells[i].NumberOfLines = metadata.NumberOfLines;
                     spells[i].ManaCost = metadata.ManaCost;
                     spells[i].Cooldown = metadata.Cooldown;
                     spells[i].CanImprove = metadata.CanImprove;

                     DateTime timestamp = DateTime.MinValue;
                     if (spells[i].Cooldown > TimeSpan.Zero && spellCooldownTimestamps.TryGetValue(name, out timestamp))
                     {
                        var elapsed = DateTime.Now - timestamp;
                        spells[i].IsOnCooldown = elapsed < (spells[i].Cooldown + TimeSpan.FromMilliseconds(500));
                     }
                  }
                  else
                  {
                     spells[i].NumberOfLines = 1;
                     spells[i].ManaCost = 0;
                     spells[i].Cooldown = TimeSpan.Zero;
                     spells[i].CanImprove = true;
                     spells[i].IsOnCooldown = false;
                  }
               }
               catch { }
            }

            owner.HasFasSpiorad = foundFasSpiorad;
            owner.HasLyliacPlant = foundLyliacPlant;
            owner.HasLyliacVineyard = foundLyliacVineyard;
         }
      }

      public void ResetDefaults()
      {
         ActiveSpell = null;

         for (int i = 0; i < spells.Capacity; i++)
         {
            spells[i].IsEmpty = true;
            spells[i].Name = null;
         }
      }

      #region IEnumerable Methods
      public IEnumerator<Spell> GetEnumerator()
      {
         foreach (var spell in spells)
            if (!spell.IsEmpty)
               yield return spell;
      }

      IEnumerator IEnumerable.GetEnumerator()
      {
         return GetEnumerator();
      }
      #endregion
   }
}
