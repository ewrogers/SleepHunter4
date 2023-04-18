using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Xml.Serialization;

using SleepHunter.Common;
using SleepHunter.IO.Process;

namespace SleepHunter.Settings
{
    [Serializable]
    internal sealed class ClientVersion : ObservableObject
    {
        public static readonly ClientVersion AutoDetect = new ClientVersion("Auto-Detect");

        private string key;
        private string hash;
        private int versionNumber;
        private long multipleInstanceAddress;
        private long introVideoAddress;
        private long noWallAddress;
        private List<MemoryVariable> variables = new List<MemoryVariable>();

        [XmlAttribute("Key")]
        public string Key
        {
            get { return key; }
            set { SetProperty(ref key, value); }
        }

        [XmlAttribute("Hash")]
        public string Hash
        {
            get { return hash; }
            set { SetProperty(ref hash, value); }
        }

        [XmlAttribute("Value")]
        public int VersionNumber
        {
            get { return versionNumber; }
            set { SetProperty(ref versionNumber, value); }
        }

        [XmlIgnore]
        public long MultipleInstanceAddress
        {
            get { return multipleInstanceAddress; }
            set { SetProperty(ref multipleInstanceAddress, value, onChanged: (s) => { RaisePropertyChanged("MultipleInstanceAddressHex"); }); }
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
                    MultipleInstanceAddress = parsedLong;
            }
        }

        [XmlIgnore]
        public long IntroVideoAddress
        {
            get { return introVideoAddress; }
            set { SetProperty(ref introVideoAddress, value, onChanged: (s) => { RaisePropertyChanged("IntroVideoAddressHex"); }); }
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
                    IntroVideoAddress = parsedLong;
            }
        }

        [XmlIgnore]
        public long NoWallAddress
        {
            get { return noWallAddress; }
            set { SetProperty(ref noWallAddress, value, onChanged: (s) => { RaisePropertyChanged("NoWallAddressHex"); }); }
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
                    NoWallAddress = parsedLong;
            }
        }

        [XmlArray("Variables")]
        [XmlArrayItem("Static", typeof(MemoryVariable))]
        [XmlArrayItem("Dynamic", typeof(DynamicMemoryVariable))]
        [XmlArrayItem("Search", typeof(SearchMemoryVariable))]
        public List<MemoryVariable> Variables
        {
            get { return variables; }
            set { SetProperty(ref variables, value); }
        }

        private ClientVersion() :
           this(string.Empty)
        { }

        public ClientVersion(string key)
        {
            this.key = key ?? throw new ArgumentNullException(nameof(key));
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
            return Key ?? string.Empty;
        }
    }
}
