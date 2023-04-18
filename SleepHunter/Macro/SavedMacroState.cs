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
    internal sealed class SavedMacroState : ObservableObject
    {
        private string characterName;
        private ModifierKeys hotkeyModifiers;
        private Key hotkeyKey;
        private bool useLyliacVineyard;
        private bool flowerAlternateCharacters;
        private List<SavedSkillState> skills = new List<SavedSkillState>();
        private List<SavedSpellState> spells = new List<SavedSpellState>();
        private List<SavedFlowerState> flowers = new List<SavedFlowerState>();

        [XmlAttribute("Name")]
        public string CharacterName
        {
            get { return characterName; }
            set { SetProperty(ref characterName, value); }
        }

        [XmlAttribute(nameof(HotkeyModifiers))]
        [DefaultValue(ModifierKeys.None)]
        public ModifierKeys HotkeyModifiers
        {
            get { return hotkeyModifiers; }
            set { SetProperty(ref hotkeyModifiers, value); }
        }

        [XmlAttribute("Hotkey")]
        [DefaultValue(Key.None)]
        public Key HotkeyKey
        {
            get { return hotkeyKey; }
            set { SetProperty(ref hotkeyKey, value); }
        }

        [XmlAttribute(nameof(UseLyliacVineyard))]
        [DefaultValue(false)]
        public bool UseLyliacVineyard
        {
            get { return useLyliacVineyard; }
            set { SetProperty(ref useLyliacVineyard, value); }
        }

        [XmlAttribute(nameof(FlowerAlternateCharacters))]
        [DefaultValue(false)]
        public bool FlowerAlternateCharacters
        {
            get { return flowerAlternateCharacters; }
            set { SetProperty(ref flowerAlternateCharacters, value); }
        }

        [XmlArray(nameof(Skills))]
        [XmlArrayItem("Skill")]
        public List<SavedSkillState> Skills
        {
            get { return skills; }
            private set { SetProperty(ref skills, value); }
        }

        [XmlArray(nameof(Spells))]
        [XmlArrayItem("Spell")]
        public List<SavedSpellState> Spells
        {
            get { return spells; }
            private set { SetProperty(ref spells, value); }
        }

        [XmlArray(nameof(Flowers))]
        [XmlArrayItem("Flower")]
        public List<SavedFlowerState> Flowers
        {
            get { return flowers; }
            private set { SetProperty(ref flowers, value); }
        }

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
