using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SleepHunter.Metadata
{
    [Serializable]
    [XmlRoot("SkillMetadata")]
    public sealed class SkillMetadataCollection
    {
        private readonly List<SkillMetadata> skills = new List<SkillMetadata>();

        [XmlAttribute("FileVersion")]
        [DefaultValue(null)]
        public string Version { get; set; }

        [XmlIgnore]
        public int Count => skills.Count;

        [XmlArray(nameof(Skills))]
        [XmlArrayItem("Skill")]
        public List<SkillMetadata> Skills => skills;

        public SkillMetadataCollection() { }

        public SkillMetadataCollection(IEnumerable<SkillMetadata> collection)
        {
            if (collection != null)
                skills.AddRange(collection);
        }
    }
}
