using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SleepHunter.Services.Serialization
{
    [Serializable]
    public sealed class SerializedLocalStorage
    {
        [XmlArray("Entries")]
        [XmlArrayItem("Entry")]
        public List<SerializedKeyValue<string, string>> Entries { get; set; } = new();

        public SerializedLocalStorage() { }

        public SerializedLocalStorage(IDictionary<string, string> collection)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            foreach (var pair in collection)
                Entries.Add(new SerializedKeyValue<string, string> { Key = pair.Key, Value = pair.Value });
        }
    }
}
