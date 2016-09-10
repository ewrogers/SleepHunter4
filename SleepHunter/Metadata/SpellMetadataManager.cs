using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace SleepHunter.Metadata
{
    public sealed class SpellMetadataManager
   {
      public static readonly string SpellMetadataFile = @"Spells.xml";

      #region Singleton
      static readonly SpellMetadataManager instance = new SpellMetadataManager();

      public static SpellMetadataManager Instance { get { return instance; } }

      private SpellMetadataManager() { }
      #endregion

      ConcurrentDictionary<string, SpellMetadata> spells = new ConcurrentDictionary<string, SpellMetadata>(StringComparer.OrdinalIgnoreCase);

      public event SpellMetadataEventHandler SpellAdded;
      public event SpellMetadataEventHandler SpellChanged;
      public event SpellMetadataEventHandler SpellRemoved;

      #region Collection Properties
      public int Count { get { return spells.Count; } }

      public IEnumerable<SpellMetadata> Spells
      {
         get { return from s in spells.Values orderby s.Name select s; }
      }
      #endregion

      #region Collection Methods
      public void AddSpell(SpellMetadata spell)
      {
         if (spell == null)
            throw new ArgumentNullException("spell");

         string spellName = spell.Name.Trim();
         bool wasUpdated = false;

         if (spells.ContainsKey(spellName))
            wasUpdated = true;

         spells[spellName] = spell;

         if (wasUpdated)
            OnSpellChanged(spell);
         else
            OnSpellAdded(spell);
      }

      public bool ContainsSpell(string spellName)
      {
         spellName = spellName.Trim();

         return spells.ContainsKey(spellName);
      }

      public SpellMetadata GetSpell(string spellName)
      {
         spellName = spellName.Trim();

         SpellMetadata spell;
         spells.TryGetValue(spellName, out spell);

         return spell;
      }

      public bool RemoveSpell(string spellName)
      {
         spellName = spellName.Trim();

         SpellMetadata removedSpell;

         var wasRemoved = spells.TryRemove(spellName, out removedSpell);

         if (wasRemoved)
            OnSpellRemoved(removedSpell);

         return wasRemoved;
      }

      public bool RenameSpell(string originalName, string newName)
      {
         SpellMetadata spell = null;
         var wasFound = spells.TryRemove(originalName, out spell);

         if (wasFound)
         {
            OnSpellRemoved(spell);
            spells[newName] = spell;
            OnSpellAdded(spell);
         }

         return wasFound;
      }

      public void ClearSpells()
      {
         foreach (var spell in spells.Values)
            OnSpellRemoved(spell);

         spells.Clear();
      }
      #endregion

      #region File Load/Save Methods
      public void LoadFromFile(string filename)
      {
         using (var inputStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
         {
            LoadFromStream(inputStream);
         }
      }

      public void LoadFromStream(Stream stream)
      {
         var serializer = new XmlSerializer(typeof(SpellMetadataCollection));
         var collection = serializer.Deserialize(stream) as SpellMetadataCollection;

         if (collection != null)
            foreach (var spell in collection.Spells)
               AddSpell(spell);
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
         var collection = new SpellMetadataCollection(this.Spells);
         var serializer = new XmlSerializer(typeof(SpellMetadataCollection));
         var namespaces = new XmlSerializerNamespaces();
         namespaces.Add("", "");

         serializer.Serialize(stream, collection, namespaces);
      }
      #endregion

      #region Event Handler Methods
      void OnSpellAdded(SpellMetadata spell)
      {
         var handler = this.SpellAdded;

         if (handler != null)
            handler(this, new SpellMetadataEventArgs(spell));
      }

      void OnSpellChanged(SpellMetadata spell)
      {
         var handler = this.SpellChanged;

         if (handler != null)
            handler(this, new SpellMetadataEventArgs(spell));
      }

      void OnSpellRemoved(SpellMetadata spell)
      {
         var handler = this.SpellRemoved;

         if (handler != null)
            handler(this, new SpellMetadataEventArgs(spell));
      }
      #endregion
   }
}
