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
        private readonly List<StaffMetadata> staves = new List<StaffMetadata>();

        [XmlAttribute("FileVersion")]
        [DefaultValue(null)]
        public string Version { get; set; }

        [XmlIgnore]
        public int Count => staves.Count;

        [XmlArray(nameof(Staves))]
        [XmlArrayItem("Staff")]
        public List<StaffMetadata> Staves => staves;

        public StaffMetadataCollection() { }


        public StaffMetadataCollection(IEnumerable<StaffMetadata> collection)
        {
            if (collection != null)
                staves.AddRange(collection);
        }

        public override string ToString() => $"Staves = {Count}";
    }
}
