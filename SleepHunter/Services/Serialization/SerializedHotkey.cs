using System;
using System.ComponentModel;
using System.Windows.Input;
using System.Xml.Serialization;

namespace SleepHunter.Services.Serialization
{
    [Serializable]
    public sealed class SerializedHotkey
    {
        [XmlAttribute]
        [DefaultValue(Key.None)]
        public Key Key { get; set; }

        [XmlAttribute]
        [DefaultValue(ModifierKeys.None)]
        public ModifierKeys Modifiers { get; set; }

        public override string ToString() => Modifiers != ModifierKeys.None ? $"{Modifiers} + {Key}" : Key.ToString();
    }
}
