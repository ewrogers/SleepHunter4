﻿using System;
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
        public static readonly ClientVersion AutoDetect = new("Auto-Detect");

        private string key;
        private string hash;
        private int versionNumber;
        private long multipleInstanceAddress;
        private long introVideoAddress;
        private long noWallAddress;
        private List<MemoryVariable> variables = new();

        [XmlAttribute("Key")]
        public string Key
        {
            get => key;
            set => SetProperty(ref key, value);
        }

        [XmlAttribute("Hash")]
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
            set => SetProperty(ref multipleInstanceAddress, value, onChanged: (s) => { RaisePropertyChanged(nameof(MultipleInstanceAddressHex)); });
        }

        [XmlElement("MultipleInstanceAddress")]
        [DefaultValue("0")]
        public string MultipleInstanceAddressHex
        {
            get => multipleInstanceAddress.ToString("X");
            set
            {
                if (long.TryParse(value, NumberStyles.HexNumber, null, out var parsedLong))
                    MultipleInstanceAddress = parsedLong;
            }
        }

        [XmlIgnore]
        public long IntroVideoAddress
        {
            get => introVideoAddress;
            set => SetProperty(ref introVideoAddress, value, onChanged: (s) => { RaisePropertyChanged(nameof(IntroVideoAddressHex)); });
        }

        [XmlElement("IntroVideoAddress")]
        [DefaultValue("0")]
        public string IntroVideoAddressHex
        {
            get => introVideoAddress.ToString("X");
            set
            {
                if (long.TryParse(value, NumberStyles.HexNumber, null, out var parsedLong))
                    IntroVideoAddress = parsedLong;
            }
        }

        [XmlIgnore]
        public long NoWallAddress
        {
            get => noWallAddress;
            set => SetProperty(ref noWallAddress, value, onChanged: (s) => { RaisePropertyChanged(nameof(NoWallAddressHex)); });
        }

        [XmlElement("NoWallAddress")]
        [DefaultValue("0")]
        public string NoWallAddressHex
        {
            get => noWallAddress.ToString("X");
            set
            {
                if (long.TryParse(value, NumberStyles.HexNumber, null, out var parsedLong))
                    NoWallAddress = parsedLong;
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

        public override string ToString() => Key ?? string.Empty;
    }
}
