using System;
using System.ComponentModel;
using System.Globalization;
using System.Xml;
using System.Xml.Serialization;

namespace SleepHunter.IO.Process
{
    [Serializable]
    public class MemoryVariable
    {
        protected long address;

        [XmlAttribute(nameof(Key))]
        public string Key { get; set; }

        [XmlIgnore]
        public long Address { get; set; }

        [XmlAttribute("Address")]
        public string AddressHex
        {
            get => $"{AddressHex:X8}";
            set
            {
                if (!long.TryParse(value, NumberStyles.HexNumber, null, out var parsedLong))
                    throw new FormatException("Invalid hex format");

                address = parsedLong;
            }
        }

        [XmlAttribute(nameof(MaxLength))]
        [DefaultValue(0)]
        public int MaxLength { get; set; }

        [XmlAttribute(nameof(Size))]
        [DefaultValue(0)]
        public int Size { get; set; }

        [XmlAttribute(nameof(Count))]
        [DefaultValue(0)]
        public int Count { get; set; }

        public MemoryVariable()
           : this(string.Empty, 0, 0) { }

        public MemoryVariable(string key, long address, int maxLength = 0, int size = 0, int count = 0)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or whitespace", nameof(key));

            Key = key;
            MaxLength = maxLength;
            Size = size;
            Count = count;

            this.address = address;
        }

        public override string ToString() => Key;
    }
}
