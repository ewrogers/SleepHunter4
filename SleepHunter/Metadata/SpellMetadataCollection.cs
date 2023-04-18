using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SleepHunter.Metadata
{
    [Serializable]
    [XmlRoot("SpellMetadata")]
    internal sealed class SpellMetadataCollection
    {
        private string version;
        private List<SpellMetadata> spells;

        [XmlAttribute("FileVersion")]
        [DefaultValue(null)]
        public string Version
        {
            get { return version; }
            set { version = value; }
        }

        [XmlIgnore]
        public int Count { get { return spells.Count; } }

        [XmlArray("Spells")]
        [XmlArrayItem("Spell")]
        public List<SpellMetadata> Spells
        {
            get { return spells; }
            private set { spells = value; }
        }

        public SpellMetadataCollection()
        {
            spells = new List<SpellMetadata>();
        }

        public SpellMetadataCollection(int capacity)
        {
            spells = new List<SpellMetadata>(capacity);
        }

        public SpellMetadataCollection(IEnumerable<SpellMetadata> collection)
           : this()
        {
            if (collection != null)
                spells.AddRange(collection);
        }
    }
}
