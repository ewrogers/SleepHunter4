﻿using System;
using System.Text.RegularExpressions;
using System.Windows.Media;

using SleepHunter.Common;

namespace SleepHunter.Models
{
    internal delegate void AbilityCallback(Ability ability);

    internal abstract class Ability : ObservableObject
    {
        static readonly Regex TrimLevelRegex = new Regex(@"^(?<name>.*)\(Lev:(?<current>[0-9]{1,})/(?<max>[0-9]{1,})\)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

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
            get { return isEmpty; }
            set { SetProperty(ref isEmpty, value); }
        }

        public int Slot
        {
            get { return slot; }
            set
            {
                SetProperty(ref slot, value, onChanged: (s) => { RaisePropertyChanged("RelativeSlot"); });
            }
        }

        public int RelativeSlot
        {
            get { return slot % 36; }
        }

        public InterfacePanel Panel
        {
            get { return panel; }
            set
            {
                SetProperty(ref panel, value, onChanged: (s) => { RaisePropertyChanged("IsSkill"); RaisePropertyChanged("IsSpell"); });
            }
        }

        public bool IsSkill
        {
            get { return panel.IsSkillPanel(); }
        }

        public bool IsSpell
        {
            get { return panel.IsSpellPanel(); }
        }

        public bool IsActive
        {
            get { return isActive; }
            set { SetProperty(ref isActive, value); }
        }

        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        public int IconIndex
        {
            get { return iconIndex; }
            set { SetProperty(ref iconIndex, value); }
        }

        public ImageSource Icon
        {
            get { return icon; }
            set { SetProperty(ref icon, value); }
        }

        public bool IsOnCooldown
        {
            get { return isOnCooldown; }
            set { SetProperty(ref isOnCooldown, value); }
        }

        public TimeSpan Cooldown
        {
            get { return cooldown; }
            set { SetProperty(ref cooldown, value); }
        }

        public int CurrentLevel
        {
            get { return currentLevel; }
            set { SetProperty(ref currentLevel, value); }
        }

        public int MaximumLevel
        {
            get { return maximumLevel; }
            set { SetProperty(ref maximumLevel, value); }
        }

        public int NumberOfLines
        {
            get { return numberOfLines; }
            set { SetProperty(ref numberOfLines, value); }
        }

        public int ManaCost
        {
            get { return manaCost; }
            set { SetProperty(ref manaCost, value); }
        }

        public bool CanImprove
        {
            get { return canImprove; }
            set { SetProperty(ref canImprove, value); }
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

            var match = TrimLevelRegex.Match(skillSpellText);

            if (!match.Success)
                return false;

            name = match.Groups["name"].Value.Trim();
            int.TryParse(match.Groups["current"].Value, out currentLevel);
            int.TryParse(match.Groups["max"].Value, out maximumLevel);

            return true;
        }
    }
}
