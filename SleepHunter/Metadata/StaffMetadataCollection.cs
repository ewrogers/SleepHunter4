using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SleepHunter.Metadata
{
    [Serializable]
    [XmlRoot("StaffMetadata")]
    public sealed class StaffMetadataCollection
    {
        private string version;
        private List<StaffMetadata> staves;

        [XmlAttribute("FileVersion")]
        [DefaultValue(null)]
        public string Version
        {
            get => version;
            set => version = value;
        }

        [XmlArray("Staves")]
        [XmlArrayItem("Staff")]
        public List<StaffMetadata> Staves
        {
            get => staves;
            private set => staves = value;
        }

        [XmlIgnore]
        public int Count => staves.Count;

        public StaffMetadataCollection()
        {
            staves = new List<StaffMetadata>();
        }

        public StaffMetadataCollection(int capacity)
        {
            staves = new List<StaffMetadata>(capacity);
        }

        public StaffMetadataCollection(IEnumerable<StaffMetadata> collection)
        {
            staves = new List<StaffMetadata>(collection);
        }

        public override string ToString() => $"Count = {Count}";
    }
}
