using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SleepHunter.Updates
{
   public delegate void SoftwareBundleCallback(SoftwareBundle bundle);

   [Serializable]
   [XmlRoot("SoftwareBundle")]
   public sealed class SoftwareBundle
   {
      string repositoryUrl;
      List<SoftwareBundleFile> files;

      [XmlAttribute("Repository")]
      public string RepositoryUrl
      {
         get { return repositoryUrl; }
         set { repositoryUrl = value; }
      }

      [XmlArray("Files")]
      [XmlArrayItem("File")]
      public List<SoftwareBundleFile> Files
      {
         get { return files; }
         private set { files = value; }
      }

      public SoftwareBundle()
      {
         files = new List<SoftwareBundleFile>();
      }

      public SoftwareBundle(int capacity)
      {
         files = new List<SoftwareBundleFile>(capacity);
      }

      public SoftwareBundle(IEnumerable<SoftwareBundleFile> collection)
      {
         files = new List<SoftwareBundleFile>(collection);
      }

      public static SoftwareBundle LoadFromFile(string file)
      {
         using (var inputStream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read))
         {
            var bundle = LoadFromStream(inputStream);
            inputStream.Close();

            return bundle;
         }
      }

      public static SoftwareBundle LoadFromStream(Stream stream)
      {
         var serializer = new XmlSerializer(typeof(SoftwareBundle));
         var bundle = serializer.Deserialize(stream) as SoftwareBundle;

         return bundle;
      }

      public void SaveToFile(string file)
      {
         using (var outputStream = File.Create(file))
         {
            SaveToStream(outputStream);
            outputStream.Close();
         }
      }

      public void SaveToStream(Stream stream)
      {
         var serializer = new XmlSerializer(typeof(SoftwareBundle));
         var namespaces = new XmlSerializerNamespaces();
         namespaces.Add("", "");

         serializer.Serialize(stream, this, namespaces);
      }
   }
}
