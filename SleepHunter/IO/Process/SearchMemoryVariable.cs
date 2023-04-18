using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SleepHunter.IO.Process
{
    [Serializable]
    internal class SearchMemoryVariable : DynamicMemoryVariable
    {
        protected MemoryOffset offset = new MemoryOffset();

        [XmlIgnore]
        public MemoryOffset Offset
        {
            get { return offset; }
            private set { offset = value; }
        }

        [XmlAttribute("Offset")]
        public string OffsetString
        {
            get { return offset.OffsetHex; }
            set { offset.OffsetHex = value; }
        }

        [XmlAttribute("IsNegative")]
        [DefaultValue(false)]
        public bool IsOffsetNegative
        {
            get { return offset.IsNegative; }
            set { offset.IsNegative = value; }
        }

        public SearchMemoryVariable()
           : this(null, 0, 0, false) { }

        public SearchMemoryVariable(string key, long address, long offset, bool isNegative = false, int maxLength = 0, int size = 0, int count = 0, params long[] offsets)
           : base(key, address, maxLength, size, count, offsets)
        {
            Offset.Offset = offset;
            Offset.IsNegative = isNegative;
        }
    }
}
