using System;
using System.Text.RegularExpressions;
using System.Windows.Media;

using SleepHunter.Common;

namespace SleepHunter.Models
{
    public delegate void AbilityCallback(Ability ability);

    public abstract class Ability : ObservableObject
    {
        private static readonly Regex AbilityWithoutLevelRegex = new(@"^(?<name>[ a-z0-9'_-]+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex AbilityWithLevelRegex = new(@"^(?<name>[ a-z0-9'_-]+)\s*\(Lev:(?<current>[0-9]{1,})/(?<max>[0-9]{1,})\)$", RegexOptions.IgnoreCase| RegexOptions.Compiled);

        private bool isEmpty;
        private int slot;
        private InterfacePanel panel;
        private string name;
        private int iconIndex;
        private ImageSource icon;
        private TimeSpan cooldown;
        private bool isOnCooldown;
        private int currentLevel;
        private int maximumLevel;
        private int numberOfLines;
        private int manaCost;
        private bool canImprove;
        private bool isActive;

        public bool IsEmpty
        {
            get => isEmpty;
            set => SetProperty(ref isEmpty, value);
        }

        public int Slot
        {
            get => slot;
            set => SetProperty(ref slot, value, onChanged: (s) => { RaisePropertyChanged(nameof(RelativeSlot)); });
        }

        public int RelativeSlot => slot % 36;

        public InterfacePanel Panel
        {
            get => panel;
            set => SetProperty(ref panel, value, onChanged: (s) => { RaisePropertyChanged(nameof(IsSkill)); RaisePropertyChanged(nameof(IsSpell)); });
        }

        public bool IsSkill => panel.IsSkillPanel();
        public bool IsSpell => panel.IsSpellPanel();

        public bool IsActive
        {
            get => isActive;
            set => SetProperty(ref isActive, value);
        }

        public string Name
        {
            get => name;
            set => SetProperty(ref name, value);
        }

        public int IconIndex
        {
            get => iconIndex;
            set => SetProperty(ref iconIndex, value);
        }

        public ImageSource Icon
        {
            get => icon;
            set => SetProperty(ref icon, value);
        }

        public bool IsOnCooldown
        {
            get => isOnCooldown;
            set => SetProperty(ref isOnCooldown, value);
        }

        public TimeSpan Cooldown
        {
            get => cooldown;
            set => SetProperty(ref cooldown, value);
        }

        public int CurrentLevel
        {
            get => currentLevel;
            set => SetProperty(ref currentLevel, value);
        }

        public int MaximumLevel
        {
            get => maximumLevel;
            set => SetProperty(ref maximumLevel, value);
        }

        public int NumberOfLines
        {
            get => numberOfLines;
            set => SetProperty(ref numberOfLines, value);
        }

        public int ManaCost
        {
            get => manaCost;
            set => SetProperty(ref manaCost, value);
        }

        public bool CanImprove
        {
            get => canImprove;
            set => SetProperty(ref canImprove, value);
        }

        public static InterfacePanel GetSkillPanelForSlot(int slot)
        {
            if (slot <= 36)
                return InterfacePanel.TemuairSkills;

            if (slot <= 72)
                return InterfacePanel.MedeniaSkills;

            return InterfacePanel.WorldSkills;
        }

        public static InterfacePanel GetSpellPanelForSlot(int slot)
        {
            if (slot <= 36)
                return InterfacePanel.TemuairSpells;

            if (slot <= 72)
                return InterfacePanel.MedeniaSpells;

            return InterfacePanel.WorldSpells;
        }

        public static bool TryParseLevels(string skillSpellText, out string name, out int currentLevel, out int maximumLevel)
        {
            name = null;
            currentLevel = 0;
            maximumLevel = 0;

            var match = AbilityWithLevelRegex.Match(skillSpellText);

            if (match.Success)
            {
                name = match.Groups["name"].Value.Trim();
                _ = int.TryParse(match.Groups["current"].Value, out currentLevel);
                _ = int.TryParse(match.Groups["max"].Value, out maximumLevel);
                return true;
            }

            match = AbilityWithoutLevelRegex.Match(skillSpellText);
            if (match.Success)
            {
                name = match.Groups["name"].Value.Trim();
                currentLevel = 0;
                maximumLevel = 0;
                return true;
            }

            return false;
        }
    }
}
