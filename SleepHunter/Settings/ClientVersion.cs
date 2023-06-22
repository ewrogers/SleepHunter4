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
        private const string DefaultExecutableName = "Darkages.exe";
        private const string DefaultWindowClassName = "DarkAges";

        public static readonly ClientVersion AutoDetect = new("Auto-Detect");

        private string key;
        private bool isDefault;
        private ClientSignature signature;
        private string executableName = DefaultExecutableName;
        private string windowClassName = DefaultWindowClassName;
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

        [XmlAttribute("IsDefault")]
        public bool IsDefault
        {
            get => isDefault;
            set => SetProperty(ref isDefault, value);
        }

        [XmlElement("Signature")]
        public ClientSignature Signature
        {
            get => signature;
            set => SetProperty(ref signature, value);
        }

        [XmlElement]
        public string ExecutableName
        {
            get => executableName;
            set => SetProperty(ref executableName, value);
        }

        [XmlElement]
        public string WindowClassName
        {
            get => windowClassName;
            set => SetProperty(ref windowClassName, value);
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

        public bool TryGetVariable(string key, out MemoryVariable variable)
        {
            variable = GetVariable(key);
            return variable != null;
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
