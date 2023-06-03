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
        protected string key;
        protected long address;
        protected int maxLength;
        protected int size;
        protected int count;

        [XmlAttribute("Key")]
        public string Key
        {
            get => key;
            set => key = value;
        }

        [XmlIgnore]
        public long Address
        {
            get => address;
            set => address = value;
        }

        [XmlAttribute("Address")]
        public string AddressHex
        {
            get => address.ToString("X");
            set
            {
                if (long.TryParse(value, NumberStyles.HexNumber, null, out var parsedLong))
                    address = parsedLong;
            }
        }

        [XmlAttribute("MaxLength")]
        [DefaultValue(0)]
        public int MaxLength
        {
            get => maxLength;
            set => maxLength = value;
        }

        [XmlAttribute("Size")]
        [DefaultValue(0)]
        public int Size
        {
            get => size;
            set => size = value;
        }

        [XmlAttribute("Count")]
        [DefaultValue(0)]
        public int Count
        {
            get => count;
            set => count = value;
        }

        public MemoryVariable()
           : this(string.Empty, 0, 0) { }

        public MemoryVariable(string key, long address, int maxLength = 0, int size = 0, int count = 0)
        {
            this.key = key;
            this.address = address;
            this.maxLength = maxLength;
            this.size = size;
            this.count = count;
        }

        public override string ToString() => Key ?? string.Empty;
    }
}
