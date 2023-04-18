using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace SleepHunter.Metadata
{
    internal sealed class SpellMetadataManager
    {
        public static readonly string SpellMetadataFile = @"Spells.xml";

        private static readonly SpellMetadataManager instance = new SpellMetadataManager();

        public static SpellMetadataManager Instance { get { return instance; } }

        private SpellMetadataManager() { }

        private readonly ConcurrentDictionary<string, SpellMetadata> spells = new ConcurrentDictionary<string, SpellMetadata>(StringComparer.OrdinalIgnoreCase);

        public event SpellMetadataEventHandler SpellAdded;
        public event SpellMetadataEventHandler SpellChanged;
        public event SpellMetadataEventHandler SpellRemoved;

        public int Count { get { return spells.Count; } }

        public IEnumerable<SpellMetadata> Spells
        {
            get { return from s in spells.Values orderby s.Name select s; }
        }

        public void AddSpell(SpellMetadata spell)
        {
            if (spell == null)
                throw new ArgumentNullException("spell");

            var spellName = spell.Name.Trim();
            var wasUpdated = false;

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

            spells.TryGetValue(spellName, out var spell);
            return spell;
        }

        public bool RemoveSpell(string spellName)
        {
            spellName = spellName.Trim();

            var wasRemoved = spells.TryRemove(spellName, out var removedSpell);

            if (wasRemoved)
                OnSpellRemoved(removedSpell);

            return wasRemoved;
        }

        public bool RenameSpell(string originalName, string newName)
        {
            var wasFound = spells.TryRemove(originalName, out var spell);

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

            if (serializer.Deserialize(stream) is SpellMetadataCollection collection)
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

        private void OnSpellAdded(SpellMetadata spell) => SpellAdded?.Invoke(this, new SpellMetadataEventArgs(spell));

        private void OnSpellChanged(SpellMetadata spell) => SpellChanged?.Invoke(this, new SpellMetadataEventArgs(spell));

        private void OnSpellRemoved(SpellMetadata spell) => SpellRemoved?.Invoke(this, new SpellMetadataEventArgs(spell));
    }
}
