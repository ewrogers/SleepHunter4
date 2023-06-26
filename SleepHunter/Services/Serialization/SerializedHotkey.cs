using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml.Serialization;

namespace SleepHunter.Services.Serialization
{
    [Serializable]
    public sealed class SerializedHotkey
    {
        [XmlAttribute("Key")]
        [DefaultValue(Key.None)]
        public Key Key { get; set; }

        [XmlAttribute("Modifiers")]
        [DefaultValue(ModifierKeys.None)]
        public ModifierKeys Modifiers {  get; set; }
    }
}
