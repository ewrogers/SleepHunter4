using System;
using System.ComponentModel;
using System.Globalization;
using System.Xml.Serialization;

namespace SleepHunter.IO.Process
{
    [Serializable]
    public sealed class MemoryOffset
    {
        private long offset;
        private bool isNegative;

        [XmlIgnore]
        public long Offset
        {
            get => offset;
            set
            {
                isNegative = value < 0;
                offset = Math.Abs(value);
            }
        }

        [XmlAttribute("Value")]
        public string OffsetHex
        {
            get => offset.ToString("X");
            set
            {
                if (long.TryParse(value, NumberStyles.HexNumber, null, out var parsedLong))
                    offset = parsedLong;
            }
        }

        [XmlAttribute("IsNegative")]
        [DefaultValue(false)]
        public bool IsNegative
        {
            get => isNegative;
            set => isNegative = value;
        }

        public MemoryOffset()
           : this(0) { }

        public MemoryOffset(long offset)
        {
            Offset = offset;
        }

        public override string ToString() => OffsetHex;
    }
}
