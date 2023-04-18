using System;
using System.ComponentModel;
using System.Globalization;
using System.Xml.Serialization;

namespace SleepHunter.IO.Process
{
    [Serializable]
    internal sealed class MemoryOffset
    {
        private long offset;

        [XmlIgnore]
        public long Offset
        {
            get => offset;
            set
            {
                IsNegative = value < 0;
                offset = Math.Abs(value);
            }
        }

        [XmlAttribute("Value")]
        public string OffsetHex
        {
            get => $"{Offset:X}";
            set
            {
                if (!long.TryParse(value, NumberStyles.HexNumber, null, out var parsedLong))
                    throw new FormatException("Invalid hex format");

                offset = parsedLong;
            }
        }

        [XmlAttribute(nameof(IsNegative))]
        [DefaultValue(false)]
        public bool IsNegative { get; set; }

        public MemoryOffset()
           : this(0) { }

        public MemoryOffset(long offset) => Offset = offset;

        public override string ToString() => OffsetHex;
    }
}
