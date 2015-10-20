using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SleepHunter.Updates
{
   [Serializable]
   public sealed class SoftwareBundleFile
   {
      string name;
      string version;
      string build;
      string url;
      string localPath;
      string hash;
      string description;

      [XmlAttribute("Name")]
      public string Name
      {
         get { return name; }
         set { name = value; }
      }

      [XmlAttribute("Version")]
      public string Version
      {
         get { return version; }
         set { version = value; }
      }

      [XmlAttribute("Build")]
      [DefaultValue(null)]
      public string Build
      {
         get { return build; }
         set { build = value; }
      }

      [XmlAttribute("Url")]
      public string Url
      {
         get { return url; }
         set { url = value; }
      }

      [XmlAttribute("LocalPath")]
      [DefaultValue(null)]
      public string LocalPath
      {
         get { return localPath; }
         set { localPath = value; }
      }

      [XmlAttribute("Hash")]
      [DefaultValue(null)]
      public string Hash
      {
         get { return hash; }
         set { hash = value; }
      }

      [XmlElement("Description")]
      [DefaultValue(null)]
      public string Description
      {
         get { return description; }
         set { description = value; }
      }

      public SoftwareBundleFile()
      {

      }
   }
}
