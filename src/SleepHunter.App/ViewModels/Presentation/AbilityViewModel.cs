using System;
using System.Windows.Media;

using CommunityToolkit.Mvvm.ComponentModel;
using SleepHunter.Runtime.Automation.Panels;

namespace SleepHunter.ViewModels.Presentation
{
    public abstract class AbilityViewModel : ObservableObject
    {
        private bool isEmpty;
        private int slot;
        private ClientPanel panel;
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
            set
            {
                if (SetProperty(ref slot, value))
                    OnPropertyChanged(nameof(RelativeSlot));
            }
        }

        public int RelativeSlot =>
            slot <= 0
                ? slot
                : ((slot - 1) % 36) + 1;

        public ClientPanel Panel
        {
            get => panel;
            set
            {
                if (!SetProperty(ref panel, value))
                    return;

                OnPropertyChanged(nameof(IsSkill));
                OnPropertyChanged(nameof(IsSpell));
            }
        }

        public bool IsSkill =>
            panel is
                ClientPanel.TemuairSkills or
                ClientPanel.MedeniaSkills or
                ClientPanel.WorldSkills;

        public bool IsSpell =>
            panel is
                ClientPanel.TemuairSpells or
                ClientPanel.MedeniaSpells or
                ClientPanel.WorldSpells;

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

        public static ClientPanel GetSkillPanelForSlot(int slot)
        {
            if (slot <= 36)
                return ClientPanel.TemuairSkills;

            if (slot <= 72)
                return ClientPanel.MedeniaSkills;

            return ClientPanel.WorldSkills;
        }

        public static ClientPanel GetSpellPanelForSlot(int slot)
        {
            if (slot <= 36)
                return ClientPanel.TemuairSpells;

            if (slot <= 72)
                return ClientPanel.MedeniaSpells;

            return ClientPanel.WorldSpells;
        }

    }
}
