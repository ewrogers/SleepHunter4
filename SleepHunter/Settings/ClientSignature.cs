using System;
using System.ComponentModel;
using System.Globalization;
using System.Xml.Serialization;
using SleepHunter.Common;

namespace SleepHunter.Settings
{
    [Serializable]
    public sealed class ClientSignature : ObservableObject
    {
        private long address;
        private string value;

        [XmlIgnore]
        public long Address
        {
            get => address;
            set => SetProperty(ref address, value, onChanged: (s) => { RaisePropertyChanged(nameof(AddressHex)); });
        }

        [XmlAttribute("Address")]
        [DefaultValue("0")]
        public string AddressHex
        {
            get => address.ToString("X");
            set
            {
                if (long.TryParse(value, NumberStyles.HexNumber, null, out var parsedLong))
                    address = parsedLong;
            }
        }

        public string Value
        {
            get => value;
            set => SetProperty(ref this.value, value);
        }

        public ClientSignature()
            : this(0, string.Empty) { }

        public ClientSignature(long address, string value)
        {
            Address = address;
            Value = value;
        }

        public override string ToString() => $"{AddressHex} = {Value}";
    }
}
