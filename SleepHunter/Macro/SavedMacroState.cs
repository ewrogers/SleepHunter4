using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.IO;
using System.Windows.Input;
using System.Xml.Serialization;

using SleepHunter.Common;

namespace SleepHunter.Macro
{
    [Serializable]
    [XmlRoot("MacroState")]
    public sealed class SavedMacroState : ObservableObject
    {
        private string characterName;
        private ModifierKeys hotkeyModifiers;
        private Key hotkeyKey;
        private bool useLyliacVineyard;
        private bool flowerAlternateCharacters;
        private readonly List<SavedSkillState> skills = new List<SavedSkillState>();
        private readonly List<SavedSpellState> spells = new List<SavedSpellState>();
        private readonly List<SavedFlowerState> flowers = new List<SavedFlowerState>();

        [XmlAttribute("Name")]
        public string CharacterName
        {
            get => characterName;
            set => SetProperty(ref characterName, value);
        }

        [XmlAttribute(nameof(HotkeyModifiers))]
        [DefaultValue(ModifierKeys.None)]
        public ModifierKeys HotkeyModifiers
        {
            get => hotkeyModifiers;
            set => SetProperty(ref hotkeyModifiers, value);
        }

        [XmlAttribute("Hotkey")]
        [DefaultValue(Key.None)]
        public Key HotkeyKey
        {
            get => hotkeyKey;
            set => SetProperty(ref hotkeyKey, value);
        }

        [XmlAttribute(nameof(UseLyliacVineyard))]
        [DefaultValue(false)]
        public bool UseLyliacVineyard
        {
            get => useLyliacVineyard;
            set => SetProperty(ref useLyliacVineyard, value);
        }

        [XmlAttribute(nameof(FlowerAlternateCharacters))]
        [DefaultValue(false)]
        public bool FlowerAlternateCharacters
        {
            get => flowerAlternateCharacters;
            set => SetProperty(ref flowerAlternateCharacters, value);
        }

        [XmlArray(nameof(Skills))]
        [XmlArrayItem("Skill")]
        public List<SavedSkillState> Skills => skills;


        [XmlArray(nameof(Spells))]
        [XmlArrayItem("Spell")]
        public List<SavedSpellState> Spells => spells;


        [XmlArray(nameof(Flowers))]
        [XmlArrayItem("Flower")]
        public List<SavedFlowerState> Flowers => flowers;

        public SavedMacroState(PlayerMacroState macroState)
        {
            if (macroState == null)
                throw new ArgumentNullException(nameof(macroState));

            CharacterName = macroState.Client.Name;

            if (macroState.Client.HasHotkey)
            {
                HotkeyModifiers = macroState.Client.Hotkey.Modifiers;
                HotkeyKey = macroState.Client.Hotkey.Key;
            }

            UseLyliacVineyard = macroState.UseLyliacVineyard;
            FlowerAlternateCharacters = macroState.FlowerAlternateCharacters;

            foreach (var skillName in macroState.Client.Skillbook.ActiveSkills)
                skills.Add(new SavedSkillState(skillName));

            foreach (var spell in macroState.QueuedSpells)
                spells.Add(new SavedSpellState(spell));

            foreach (var flower in macroState.FlowerTargets)
                flowers.Add(new SavedFlowerState(flower));
        }

        public static SavedMacroState LoadFromFile(string file)
        {
            using (var inputStream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                return LoadFromStream(inputStream);
        }

        public static SavedMacroState LoadFromStream(Stream stream)
        {
            var serializer = new XmlSerializer(typeof(SavedMacroState));
            var state = serializer.Deserialize(stream) as SavedMacroState;

            return state;
        }

        public void SaveToFile(string file)
        {
            using (var outputStream = File.Create(file))
                SaveToStream(outputStream);
        }

        public void SaveToStream(Stream stream)
        {
            var serializer = new XmlSerializer(typeof(SavedMacroState));
            var namespaces = new XmlSerializerNamespaces();
            namespaces.Add("", "");

            serializer.Serialize(stream, this, namespaces);
        }
    }
}
