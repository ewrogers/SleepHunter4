using System;
using System.Windows.Media;

using SleepHunter.Common;

namespace SleepHunter.Models
{
    public abstract class Ability : ObservableObject
    {
        private bool isEmpty;
        private int slot;
        private InterfacePanel panel;
        private string name;
        private ImageSource icon;
        private TimeSpan cooldown;
        private bool isOnCooldown;
        private int currentLevel;
        private int maximumLevel;
        private int numberOfLines;
        private int manaCost;
        private bool canImprove;
        private bool isActive;
        private bool hasClientCooldownProgress;
        private double cooldownRemainingFraction = 1.0;

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

        public int RelativeSlot =>
            slot <= 0
                ? slot
                : ((slot - 1) % 36) + 1;

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

        public bool HasClientCooldownProgress
        {
            get => hasClientCooldownProgress;
            set => SetProperty(ref hasClientCooldownProgress, value);
        }

        public double CooldownRemainingFraction
        {
            get => cooldownRemainingFraction;
            protected set => SetProperty(ref cooldownRemainingFraction, value);
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

    }
}
