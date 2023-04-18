using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SleepHunter.IO.Process
{
    [Serializable]
    internal class DynamicMemoryVariable : MemoryVariable
    {
        [XmlArray(nameof(Offsets))]
        [XmlArrayItem("Offset", typeof(MemoryOffset))]
        public List<MemoryOffset> Offsets { get; set; } = new List<MemoryOffset>();

        public DynamicMemoryVariable()
           : this(string.Empty, 0, 0) { }

        public DynamicMemoryVariable(string key, long address, int maxLength = 0, int size = 0, int count = 0, params long[] offsets)
           : base(key, address, maxLength, size, count)
        {
            if (offsets == null)
                return;

            foreach (var offset in offsets)
                Offsets.Add(new MemoryOffset(offset));
        }
    }
}
