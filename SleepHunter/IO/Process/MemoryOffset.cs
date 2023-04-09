using System;
using System.ComponentModel;
using System.Globalization;
using System.Xml.Serialization;

namespace SleepHunter.IO.Process
{
    [Serializable]
    public sealed class MemoryOffset
    {
        long offset;
        bool isNegative;

        [XmlIgnore]
        public long Offset
        {
            get { return offset; }
            set
            {
                isNegative = value < 0;
                offset = Math.Abs(value);
            }
        }

        [XmlAttribute("Value")]
        public string OffsetHex
        {
            get { return offset.ToString("X"); }

            set
            {
                long parsedLong;

                if (long.TryParse(value, NumberStyles.HexNumber, null, out parsedLong))
                    offset = parsedLong;
            }
        }

        [XmlAttribute("IsNegative")]
        [DefaultValue(false)]
        public bool IsNegative
        {
            get { return isNegative; }
            set { isNegative = value; }
        }

        public MemoryOffset()
           : this(0) { }

        public MemoryOffset(long offset)
        {
            Offset = offset;
        }

        public override string ToString()
        {
            return OffsetHex;
        }
    }
}
