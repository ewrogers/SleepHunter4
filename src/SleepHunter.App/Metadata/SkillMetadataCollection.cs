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
        private string version;
        private List<SkillMetadata> skills;

        [XmlAttribute("FileVersion")]
        [DefaultValue(null)]
        public string Version
        {
            get => version;
            set => version = value;
        }

        [XmlIgnore]
        public int Count => skills.Count;

        [XmlArray("Skills")]
        [XmlArrayItem("Skill")]
        public List<SkillMetadata> Skills
        {
            get => skills;
            private set => skills = value;
        }

        public SkillMetadataCollection()
        {
            skills = new List<SkillMetadata>();
        }

        public SkillMetadataCollection(int capacity)
        {
            skills = new List<SkillMetadata>(capacity);
        }

        public SkillMetadataCollection(IEnumerable<SkillMetadata> collection)
           : this()
        {
            if (collection != null)
                skills.AddRange(collection);
        }

        public override string ToString() => $"Count = {Count}";
    }
}
