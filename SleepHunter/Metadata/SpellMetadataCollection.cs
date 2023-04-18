using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SleepHunter.Metadata
{
    [Serializable]
    [XmlRoot("SpellMetadata")]
    public sealed class SpellMetadataCollection
    {
        private readonly List<SpellMetadata> spells = new List<SpellMetadata>();

        [XmlAttribute("FileVersion")]
        [DefaultValue(null)]
        public string Version { get; set; }

        [XmlIgnore]
        public int Count => spells.Count;

        [XmlArray(nameof(Spells))]
        [XmlArrayItem("Spell")]
        public List<SpellMetadata> Spells => spells;

        public SpellMetadataCollection() { } 

        public SpellMetadataCollection(IEnumerable<SpellMetadata> collection)
           : this()
        {
            if (collection != null)
                spells.AddRange(collection);
        }
    }
}
