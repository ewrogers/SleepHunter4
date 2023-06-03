using System;
using System.IO;
using System.Text;

using SleepHunter.Common;
using SleepHunter.IO.Process;

namespace SleepHunter.Models
{
    public sealed class PlayerStats : ObservableObject
    {
        private const string CurrentHealthKey = @"CurrentHealth";
        private const string MaximumHealthKey = @"MaximumHealth";
        private const string CurrentManaKey = @"CurrentMana";
        private const string MaximumManaKey = @"MaximumMana";
        private const string LevelKey = @"Level";
        private const string AbilityLevelKey = @"AbilityLevel";

        private int currentHealth;
        private int maximumHealth;
        private int currentMana;
        private int maximumMana;
        private int level;
        private int abilityLevel;

        public event EventHandler StatsUpdated;

        public Player Owner { get; }

        public int CurrentHealth
        {
            get => currentHealth;
            set => SetProperty(ref currentHealth, value, onChanged: (s) => { RaisePropertyChanged(nameof(HealthPercent)); RaisePropertyChanged(nameof(HasFullHealth)); });
        }

        public int MaximumHealth
        {
            get => maximumHealth;
            set => SetProperty(ref maximumHealth, value, onChanged: (s) => { RaisePropertyChanged(nameof(HealthPercent)); RaisePropertyChanged(nameof(HasFullHealth)); });
        }

        public bool HasFullHealth => currentHealth >= maximumHealth && currentHealth > 0;

        public int CurrentMana
        {
            get => currentMana;
            set => SetProperty(ref currentMana, value, onChanged: (s) => { RaisePropertyChanged(nameof(ManaPercent)); RaisePropertyChanged(nameof(HasFullMana)); });
        }

        public int MaximumMana
        {
            get => maximumMana;
            set => SetProperty(ref maximumMana, value, onChanged: (s) => { RaisePropertyChanged(nameof(ManaPercent)); RaisePropertyChanged(nameof(HasFullMana)); });
        }

        public bool HasFullMana => currentMana >= maximumMana && currentMana > 0;

        public double HealthPercent
        {
            get
            {
                if (maximumHealth <= 0)
                    return 0;

                if (currentHealth >= maximumHealth)
                    return 100;

                return (currentHealth * 100.0) / maximumHealth;
            }
        }

        public double ManaPercent
        {
            get
            {
                if (maximumMana <= 0)
                    return 0;

                if (currentMana >= maximumMana)
                    return 100;

                return (currentMana * 100.0) / maximumMana;
            }
        }

        public int Level
        {
            get => level;
            set => SetProperty(ref level, value);
        }

        public int AbilityLevel
        {
            get => abilityLevel;
            set => SetProperty(ref abilityLevel, value);
        }

        public PlayerStats(Player owner)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        public void Update()
        {
            Update(Owner.Accessor);
            StatsUpdated?.Invoke(this, EventArgs.Empty);
        }

        public void Update(ProcessMemoryAccessor accessor)
        {
            if (accessor == null)
                throw new ArgumentNullException(nameof(accessor));

            var version = Owner.Version;

            if (version == null)
            {
                ResetDefaults();
                return;
            }

            var currentHealthVariable = version.GetVariable(CurrentHealthKey);
            var maximumHealthVariable = version.GetVariable(MaximumHealthKey);
            var currentManaVariable = version.GetVariable(CurrentManaKey);
            var maximumManaVariable = version.GetVariable(MaximumManaKey);
            var levelVariable = version.GetVariable(LevelKey);
            var abilityLevelVariable = version.GetVariable(AbilityLevelKey);

            using var stream = accessor.GetStream();
            using var reader = new BinaryReader(stream, Encoding.ASCII);

            // Current Health
            if (currentHealthVariable != null && currentHealthVariable.TryReadIntegerString(reader, out var currentHealth))
                CurrentHealth = (int)currentHealth;
            else
                CurrentHealth = 0;

            // Max Health
            if (maximumHealthVariable != null && maximumHealthVariable.TryReadIntegerString(reader, out var maximumHealth))
                MaximumHealth = (int)maximumHealth;
            else
                MaximumHealth = 0;

            // Current Mana
            if (currentManaVariable != null && currentManaVariable.TryReadIntegerString(reader, out var currentMana))
                CurrentMana = (int)currentMana;
            else
                CurrentMana = 0;

            // Max Mana
            if (maximumManaVariable != null && maximumManaVariable.TryReadIntegerString(reader, out var maximumMana))
                MaximumMana = (int)maximumMana;
            else
                MaximumMana = 0;

            // Level
            if (levelVariable != null && levelVariable.TryReadIntegerString(reader, out var level))
                Level = (int)level;
            else
                Level = 0;

            // Ability Level
            if (abilityLevelVariable != null && abilityLevelVariable.TryReadIntegerString(reader, out var abilityLevel))
                AbilityLevel = (int)abilityLevel;
            else
                AbilityLevel = 0;
        }

        public void ResetDefaults()
        {
            CurrentHealth = 0;
            MaximumHealth = 0;
            CurrentMana = 0;
            MaximumMana = 0;
            Level = 0;
            AbilityLevel = 0;
        }
    }
}
