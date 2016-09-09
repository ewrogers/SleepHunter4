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
         get { return key; }
         set { key = value; }
      }

      [XmlIgnore]
      public long Address
      {
         get { return address; }
         set { address = value; }
      }

      [XmlAttribute("Address")]
      public string AddressHex
      {
         get { return address.ToString("X"); }
         set
         {
            long parsedLong;

            if(long.TryParse(value, NumberStyles.HexNumber, null, out parsedLong))
               address = parsedLong;
         }
      }

      [XmlAttribute("MaxLength")]
      [DefaultValue(0)]
      public int MaxLength
      {
         get { return maxLength; }
         set { maxLength = value; }
      }

      [XmlAttribute("Size")]
      [DefaultValue(0)]
      public int Size
      {
         get { return size; }
         set { size = value; }
      }

      [XmlAttribute("Count")]
      [DefaultValue(0)]
      public int Count
      {
         get { return count; }
         set { count = value; }
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

      public override string ToString()
      {
         return this.Key ?? string.Empty;
      }
   }
}
