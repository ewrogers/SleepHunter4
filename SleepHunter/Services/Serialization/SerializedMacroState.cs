using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using SleepHunter.Macro;

namespace SleepHunter.Services.Serialization
{
    [Serializable]
    [XmlRoot("MacroState")]
    public sealed class SerializedMacroState
    {
        private const string SaveFileVersion = "4.10";

        [XmlAttribute("Version")]
        public string Version { get; set; } = SaveFileVersion;

        [XmlElement("Name")]
        [DefaultValue(null)]
        public string Name { get; set; }

        [XmlElement("Description")]
        [DefaultValue(null)]
        public string Description { get; set; }

        [XmlElement("Hotkey")]
        [DefaultValue(null)]
        public SerializedHotkey Hotkey { get; set; }

        [XmlArray("Skills")]
        [XmlArrayItem("Skill")]
        public List<SerializedSkillState> Skills { get; set; } = new();

        [XmlElement("SpellRotation")]
        [DefaultValue(SpellRotationMode.Default)]
        public SpellRotationMode SpellRotation { get; set; }

        [XmlArray("Spells")]
        [XmlArrayItem("Spell")]
        public List<SerializedSpellState> Spells { get; set; } = new();

        [XmlElement("UseLyliacVineyard")]
        [DefaultValue(false)]
        public bool UseLyliacVineyard { get; set; }

        [XmlElement("FlowerAlternateCharacters")]
        [DefaultValue(false)]
        public bool FlowerAlternateCharacters { get; set; }

        [XmlArray("Flowering")]
        [XmlArrayItem("Flower")]
        public List<SerializedFlowerState> FlowerTargets { get; set; } = new();

        [XmlElement("LocalStorage")]
        public SerializedLocalStorage LocalStorage { get; set; } = new();

        public override string ToString() => Name;
    }
}
