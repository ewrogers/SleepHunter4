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
    public sealed class ClientVersion : ObservableObject
    {
        private string key;
        private string hash;
        private int versionNumber;
        private long multipleInstanceAddress;
        private long introVideoAddress;
        private long noWallAddress;
        private List<MemoryVariable> variables = new List<MemoryVariable>();

        [XmlAttribute(nameof(Key))]
        public string Key
        {
            get => key;
            set => SetProperty(ref key, value);
        }

        [XmlAttribute(nameof(Hash))]
        public string Hash
        {
            get => hash;
            set => SetProperty(ref hash, value);
        }

        [XmlAttribute("Value")]
        public int VersionNumber
        {
            get => versionNumber; 
            set => SetProperty(ref versionNumber, value);
        }

        [XmlIgnore]
        public long MultipleInstanceAddress
        {
            get => multipleInstanceAddress; 
            set => SetProperty(ref multipleInstanceAddress, value, onChanged: (s) =>
            {
                RaisePropertyChanged(nameof(MultipleInstanceAddressHex));
            });
        }

        [XmlElement("MultipleInstanceAddress")]
        [DefaultValue("0")]
        public string MultipleInstanceAddressHex
        {
            get => $"{multipleInstanceAddress:X}";
            set
            {
                if (!long.TryParse(value, NumberStyles.HexNumber, null, out var parsedLong))
                    throw new FormatException("Invalid hex format");

                MultipleInstanceAddress = parsedLong;
                OnPropertyChanged(nameof(MultipleInstanceAddress));
                OnPropertyChanged(nameof(MultipleInstanceAddressHex));
            }
        }

        [XmlIgnore]
        public long IntroVideoAddress
        {
            get => introVideoAddress;
            set => SetProperty(ref introVideoAddress, value, onChanged: (s) =>
            { 
                RaisePropertyChanged(nameof(IntroVideoAddressHex));
            });
        }

        [XmlElement("IntroVideoAddress")]
        [DefaultValue("0")]
        public string IntroVideoAddressHex
        {
            get => $"{IntroVideoAddress:X}";
            set
            {
                if (!long.TryParse(value, NumberStyles.HexNumber, null, out var parsedLong))
                    throw new FormatException("Invalid hex format");

                IntroVideoAddress = parsedLong;
                OnPropertyChanged(nameof(IntroVideoAddress));
                OnPropertyChanged(nameof(IntroVideoAddressHex));
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
            get => $"{NoWallAddress:X}";
            set
            {
                if (!long.TryParse(value, NumberStyles.HexNumber, null, out var parsedLong))
                    throw new FormatException("Invalid hex format");

                NoWallAddress = parsedLong;
                OnPropertyChanged(nameof(NoWallAddress));
                OnPropertyChanged(nameof(NoWallAddressHex));
            }
        }

        [XmlArray("Variables")]
        [XmlArrayItem("Static", typeof(MemoryVariable))]
        [XmlArrayItem("Dynamic", typeof(DynamicMemoryVariable))]
        [XmlArrayItem("Search", typeof(SearchMemoryVariable))]
        public List<MemoryVariable> Variables
        {
            get => variables;
            set => SetProperty(ref variables, value);
        }

        public ClientVersion(string key)
            => this.key = key ?? throw new ArgumentNullException(nameof(key));

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

        public override string ToString() => Key ?? string.Empty;
    }
}
