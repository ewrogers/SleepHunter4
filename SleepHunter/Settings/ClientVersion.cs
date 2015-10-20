using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

using SleepHunter.Data;
using SleepHunter.IO.Process;

namespace SleepHunter.Settings
{
   [Serializable]
   public sealed class ClientVersion : NotifyObject
   {
      public static readonly ClientVersion AutoDetect = new ClientVersion("Auto-Detect");

      string key;
      string hash;
      int versionNumber;
      long multipleInstanceAddress;
      long introVideoAddress;
      long noWallAddress;
      List<MemoryVariable> variables = new List<MemoryVariable>();

      [XmlAttribute("Key")]
      public string Key
      {
         get { return key; }
         set { SetProperty(ref key, value, "Key"); }
      }

      [XmlAttribute("Hash")]
      public string Hash
      {
         get { return hash; }
         set { SetProperty(ref hash, value, "Hash"); }
      }

      [XmlAttribute("Value")]
      public int VersionNumber
      {
         get { return versionNumber; }
         set { SetProperty(ref versionNumber, value, "VersionNumber"); }
      }

      [XmlIgnore]
      public long MultipleInstanceAddress
      {
         get { return multipleInstanceAddress; }
         set { SetProperty(ref multipleInstanceAddress, value, "MultipleInstanceAddress", onChanged: (s) => { OnPropertyChanged("MultipleInstanceAddressHex"); }); }
      }

      [XmlElement("MultipleInstanceAddress")]
      [DefaultValue("0")]
      public string MultipleInstanceAddressHex
      {
         get { return multipleInstanceAddress.ToString("X"); }
         set
         {
            long parsedLong;

            if (long.TryParse(value, NumberStyles.HexNumber, null, out parsedLong))
               this.MultipleInstanceAddress = parsedLong;
         }
      }

      [XmlIgnore]
      public long IntroVideoAddress
      {
         get { return introVideoAddress; }
         set { SetProperty(ref introVideoAddress, value, "IntroVideoAddress", onChanged: (s) => { OnPropertyChanged("IntroVideoAddressHex"); }); }
      }

      [XmlElement("IntroVideoAddress")]
      [DefaultValue("0")]
      public string IntroVideoAddressHex
      {
         get { return introVideoAddress.ToString("X"); }
         set
         {
            long parsedLong;

            if (long.TryParse(value, NumberStyles.HexNumber, null, out parsedLong))
               this.IntroVideoAddress = parsedLong;
         }
      }

      [XmlIgnore]
      public long NoWallAddress
      {
         get { return noWallAddress; }
         set { SetProperty(ref noWallAddress, value, "NoWallAddress", onChanged: (s) => { OnPropertyChanged("NoWallAddressHex"); }); }
      }

      [XmlElement("NoWallAddress")]
      [DefaultValue("0")]
      public string NoWallAddressHex
      {
         get { return noWallAddress.ToString("X"); }
         set
         {
            long parsedLong;

            if (long.TryParse(value, NumberStyles.HexNumber, null, out parsedLong))
               this.NoWallAddress = parsedLong;
         }
      }

      [XmlArray("Variables")]
      [XmlArrayItem("Static", typeof(MemoryVariable))]
      [XmlArrayItem("Dynamic", typeof(DynamicMemoryVariable))]
      [XmlArrayItem("Search", typeof(SearchMemoryVariable))]
      public List<MemoryVariable> Variables
      {
         get { return variables; }
         set { SetProperty(ref variables, value, "Variables"); }
      }

      private ClientVersion() :
         this(string.Empty) { }

      public ClientVersion(string key)
      {
         if (key == null)
            throw new ArgumentNullException("key");

         this.key = key;
      }

      public MemoryVariable GetVariable(string key)
      {
         foreach (var variable in variables)
            if (string.Equals(variable.Key, key, StringComparison.OrdinalIgnoreCase))
               return variable;

         return null;
      }

      public bool ContainsVariable(string key)
      {
         foreach (var variable in variables)
            if (string.Equals(variable.Key, key, StringComparison.OrdinalIgnoreCase))
               return true;

         return false;
      }

      public override string ToString()
      {
         return this.Key ?? string.Empty;
      }
   }
}
