using System;
using System.Xml.Serialization;

namespace SleepHunter.Services.Serialization
{
    [Serializable]
    public sealed class SerializedKeyValue<K, V>
    {
        [XmlAttribute("Key")]
        public K Key { get; set; }

        [XmlAttribute("Value")]
        public V Value { get; set; }

        public override string ToString() => $"{Key} = {Value}";
    }
}
