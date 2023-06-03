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
        private string version;
        private List<ClientVersion> versions;

        [XmlAttribute("FileVersion")]
        [DefaultValue(null)]
        public string Version
        {
            get => version;
            set => version = value;
        }

        [XmlIgnore]
        public int Count => versions.Count;

        [XmlArray("Clients")]
        [XmlArrayItem("Client")]
        public List<ClientVersion> Versions
        {
            get => versions;
            private set => versions = value;
        }

        public ClientVersionCollection()
        {
            versions = new List<ClientVersion>();
        }

        public ClientVersionCollection(int capacity)
        {
            versions = new List<ClientVersion>(capacity);
        }

        public ClientVersionCollection(IEnumerable<ClientVersion> collection)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            versions = new List<ClientVersion>(collection);
        }

        public override string ToString() => $"Count = {Count}";
    }
}
