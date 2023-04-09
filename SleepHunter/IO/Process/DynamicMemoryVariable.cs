using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SleepHunter.IO.Process
{
    [Serializable]
    public class DynamicMemoryVariable : MemoryVariable
    {
        protected List<MemoryOffset> offsets = new List<MemoryOffset>();

        [XmlArray("Offsets")]
        [XmlArrayItem("Offset", typeof(MemoryOffset))]
        public List<MemoryOffset> Offets
        {
            get { return offsets; }
            set { offsets = value; }
        }

        public DynamicMemoryVariable()
           : this(string.Empty, 0, 0) { }

        public DynamicMemoryVariable(string key, long address, int maxLength = 0, int size = 0, int count = 0, params long[] offsets)
           : base(key, address, maxLength, size, count)
        {
            if (offsets != null)
                foreach (var offset in offsets)
                    this.offsets.Add(new MemoryOffset(offset));
        }
    }
}
