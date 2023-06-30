using System;
using System.IO;
using System.Text;

using SleepHunter.Common;
using SleepHunter.IO.Process;

namespace SleepHunter.Models
{
    public sealed class PlayerStats : UpdatableObject
    {
        private const string CurrentHealthKey = @"CurrentHealth";
        private const string MaximumHealthKey = @"MaximumHealth";
        private const string CurrentManaKey = @"CurrentMana";
        private const string MaximumManaKey = @"MaximumMana";
        private const string LevelKey = @"Level";
        private const string AbilityLevelKey = @"AbilityLevel";

        private readonly Stream stream;
        private readonly BinaryReader reader;

        private int currentHealth;
        private int maximumHealth;
        private int currentMana;
        private int maximumMana;
        private int level;
        private int abilityLevel;

        public Player Owner { get; init;  }

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

            stream = owner.Accessor.GetStream();
            reader = new BinaryReader(stream, Encoding.ASCII);
        }


        protected override void OnUpdate()
        {
            var version = Owner.Version;

            if (version == null)
            {
                ResetDefaults();
                return;
            }

            var hpVariable = version.GetVariable(CurrentHealthKey);
            var maxHpVariable = version.GetVariable(MaximumHealthKey);
            var mpVariable = version.GetVariable(CurrentManaKey);
            var maxMpVariable = version.GetVariable(MaximumManaKey);
            var levelVariable = version.GetVariable(LevelKey);
            var abVariable = version.GetVariable(AbilityLevelKey);

            // Current Health
            if (hpVariable != null && hpVariable.TryReadIntegerString(reader, out var currentHealth))
                CurrentHealth = (int)currentHealth;
            else
                CurrentHealth = 0;

            // Max Health
            if (maxHpVariable != null && maxHpVariable.TryReadIntegerString(reader, out var maximumHealth))
                MaximumHealth = (int)maximumHealth;
            else
                MaximumHealth = 0;

            // Current Mana
            if (mpVariable != null && mpVariable.TryReadIntegerString(reader, out var currentMana))
                CurrentMana = (int)currentMana;
            else
                CurrentMana = 0;

            // Max Mana
            if (maxMpVariable != null && maxMpVariable.TryReadIntegerString(reader, out var maximumMana))
                MaximumMana = (int)maximumMana;
            else
                MaximumMana = 0;

            // Level
            if (levelVariable != null && levelVariable.TryReadIntegerString(reader, out var level))
                Level = (int)level;
            else
                Level = 0;

            // Ability Level
            if (abVariable != null && abVariable.TryReadIntegerString(reader, out var abilityLevel))
                AbilityLevel = (int)abilityLevel;
            else
                AbilityLevel = 0;
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposed)
                return;

            if (isDisposing)
            {
                reader?.Dispose();
                stream?.Dispose();
            }

            base.Dispose(isDisposing);
        }

        private void ResetDefaults()
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
