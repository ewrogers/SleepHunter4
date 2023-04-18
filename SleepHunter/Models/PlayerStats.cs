using System;
using System.IO;
using System.Text;

using SleepHunter.Common;
using SleepHunter.IO.Process;

namespace SleepHunter.Models
{
    internal sealed class PlayerStats : ObservableObject
    {
        private static readonly string CurrentHealthKey = @"CurrentHealth";
        private static readonly string MaximumHealthKey = @"MaximumHealth";
        private static readonly string CurrentManaKey = @"CurrentMana";
        private static readonly string MaximumManaKey = @"MaximumMana";
        private static readonly string LevelKey = @"Level";
        private static readonly string AbilityLevelKey = @"AbilityLevel";

        private Player owner;
        private int currentHealth;
        private int maximumHealth;
        private int currentMana;
        private int maximumMana;
        private int level;
        private int abilityLevel;

        public Player Owner
        {
            get { return owner; }
            set { SetProperty(ref owner, value); }
        }

        public int CurrentHealth
        {
            get { return currentHealth; }
            set
            {
                SetProperty(ref currentHealth, value, onChanged: (s) => { RaisePropertyChanged("HealthPercent"); RaisePropertyChanged("HasFullHealth"); });
            }
        }

        public int MaximumHealth
        {
            get { return maximumHealth; }
            set
            {
                SetProperty(ref maximumHealth, value, onChanged: (s) => { RaisePropertyChanged("HealthPercent"); RaisePropertyChanged("HasFullHealth"); });
            }
        }

        public bool HasFullHealth
        {
            get { return currentHealth >= maximumHealth && currentHealth > 0; }
        }

        public int CurrentMana
        {
            get { return currentMana; }
            set
            {
                SetProperty(ref currentMana, value, onChanged: (s) => { RaisePropertyChanged("ManaPercent"); RaisePropertyChanged("HasFullMana"); });
            }
        }

        public int MaximumMana
        {
            get { return maximumMana; }
            set
            {
                SetProperty(ref maximumMana, value, onChanged: (s) => { RaisePropertyChanged("ManaPercent"); RaisePropertyChanged("HasFullMana"); });
            }
        }

        public bool HasFullMana
        {
            get { return currentMana >= maximumMana && currentMana > 0; }
        }

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
            get { return level; }
            set { SetProperty(ref level, value); }
        }

        public int AbilityLevel
        {
            get { return abilityLevel; }
            set { SetProperty(ref abilityLevel, value); }
        }

        public PlayerStats()
           : this(null) { }

        public PlayerStats(Player owner)
        {
            this.owner = owner;
        }

        public void Update()
        {
            if (owner == null)
                throw new InvalidOperationException("Player owner is null, cannot update.");

            Update(owner.Accessor);
        }

        public void Update(ProcessMemoryAccessor accessor)
        {
            if (accessor == null)
                throw new ArgumentNullException("accessor");

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


            Stream stream = null;
            try
            {
                stream = accessor.GetStream();
                using (var reader = new BinaryReader(stream, Encoding.ASCII))
                {
                    stream = null;

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
            }
            finally { stream?.Dispose(); }
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
