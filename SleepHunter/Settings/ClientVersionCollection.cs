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
    string version;
    List<ClientVersion> versions;

    [XmlAttribute("FileVersion")]
    [DefaultValue(null)]
    public string Version
    {
      get { return version; }
      set { version = value; }
    }

    [XmlIgnore]
    public int Count { get { return versions.Count; } }

    [XmlArray("Clients")]
    [XmlArrayItem("Client")]
    public List<ClientVersion> Versions
    {
      get { return versions; }
      private set { versions = value; }
    }

    public ClientVersionCollection()
    {
      this.versions = new List<ClientVersion>();
    }

    public ClientVersionCollection(int capacity)
    {
      this.versions = new List<ClientVersion>(capacity);
    }

    public ClientVersionCollection(IEnumerable<ClientVersion> collection)
    {
      this.versions = new List<ClientVersion>(collection);
    }
  }
}
