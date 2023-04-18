using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SleepHunter.Settings
{
    [Serializable]
    [XmlRoot("ClientVersions")]
    public sealed class ClientVersionCollection
    {
        private readonly List<ClientVersion> versions = new List<ClientVersion>();

        [XmlAttribute("FileVersion")]
        [DefaultValue(null)]
        public string Version { get; set; }

        [XmlIgnore]
        public int Count => versions.Count;

        [XmlArray("Clients")]
        [XmlArrayItem("Client")]
        public List<ClientVersion> Versions => versions;

        public ClientVersionCollection() { }


        public ClientVersionCollection(IEnumerable<ClientVersion> collection)
        {
            if (collection != null)
                versions.AddRange(collection);
        }

        public override string ToString() => $"Versions = {Count}";
    }
}
