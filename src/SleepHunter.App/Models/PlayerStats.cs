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
        private const string TotalExperienceKey = @"TotalExperience";
        private const string StrengthKey = @"Strength";
        private const string DexterityKey = @"Dexterity";
        private const string WisdomKey = @"Wisdom";
        private const string ConstitutionKey = @"Constitution";
        private const string IntelligenceKey = @"Intelligence";
        private const string StatPointsKey = @"StatPoints";
        private const string ExperienceToNextLevelKey = @"ExperienceToNextLevel";
        private const string GamePointsKey = @"GamePoints";
        private const string AbilityToNextLevelKey = @"AbilityToNextLevel";
        private const string TotalAbilityKey = @"TotalAbility";
        private const string WeightKey = @"Weight";
        private const string MaximumWeightKey = @"MaximumWeight";
        private const string ArmorClassKey = @"ArmorClass";
        private const string DamageModifierKey = @"DamageModifier";
        private const string HitModifierKey = @"HitModifier";
        private const string AttackElementKey = @"AttackElement";
        private const string DefenseElementKey = @"DefenseElement";
        private const string MagicResistanceKey = @"MagicResistance";

        private readonly Stream stream;
        private readonly BinaryReader reader;

        private int currentHealth;
        private int maximumHealth;
        private int currentMana;
        private int maximumMana;
        private int level;
        private int abilityLevel;
        private long totalExperience;
        private int strength;
        private int dexterity;
        private int wisdom;
        private int constitution;
        private int intelligence;
        private int statPoints;
        private long experienceToNextLevel;
        private long gamePoints;
        private long abilityToNextLevel;
        private long totalAbility;
        private int weight;
        private int maximumWeight;
        private int armorClass;
        private int damageModifier;
        private int hitModifier;
        private PlayerElement? attackElement;
        private PlayerElement? defenseElement;
        private int magicResistanceUnits;

        public Player Owner { get; init; }

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

        public long TotalExperience
        {
            get => totalExperience;
            set => SetProperty(ref totalExperience, value);
        }

        public int Strength
        {
            get => strength;
            set => SetProperty(ref strength, value);
        }

        public int Dexterity
        {
            get => dexterity;
            set => SetProperty(ref dexterity, value);
        }

        public int Wisdom
        {
            get => wisdom;
            set => SetProperty(ref wisdom, value);
        }

        public int Constitution
        {
            get => constitution;
            set => SetProperty(ref constitution, value);
        }

        public int Intelligence
        {
            get => intelligence;
            set => SetProperty(ref intelligence, value);
        }

        public int StatPoints
        {
            get => statPoints;
            set => SetProperty(ref statPoints, value);
        }

        public long ExperienceToNextLevel
        {
            get => experienceToNextLevel;
            set => SetProperty(ref experienceToNextLevel, value);
        }

        public long GamePoints
        {
            get => gamePoints;
            set => SetProperty(ref gamePoints, value);
        }

        public long AbilityToNextLevel
        {
            get => abilityToNextLevel;
            set => SetProperty(ref abilityToNextLevel, value);
        }

        public long TotalAbility
        {
            get => totalAbility;
            set => SetProperty(ref totalAbility, value);
        }

        public int Weight
        {
            get => weight;
            set => SetProperty(ref weight, value);
        }

        public int MaximumWeight
        {
            get => maximumWeight;
            set => SetProperty(ref maximumWeight, value);
        }

        public int ArmorClass
        {
            get => armorClass;
            set => SetProperty(ref armorClass, value);
        }

        public int DamageModifier
        {
            get => damageModifier;
            set => SetProperty(ref damageModifier, value);
        }

        public int HitModifier
        {
            get => hitModifier;
            set => SetProperty(ref hitModifier, value);
        }

        public PlayerElement? AttackElement
        {
            get => attackElement;
            set => SetProperty(ref attackElement, value);
        }

        public PlayerElement? DefenseElement
        {
            get => defenseElement;
            set => SetProperty(ref defenseElement, value);
        }

        public int MagicResistanceUnits
        {
            get => magicResistanceUnits;
            set => SetProperty(
                ref magicResistanceUnits,
                value,
                onChanged: (_) => RaisePropertyChanged(nameof(MagicResistancePercent)));
        }

        public int MagicResistancePercent => MagicResistanceUnits * 10;

        public PlayerStats(Player owner)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));

            stream = owner.Accessor.GetStream();
            reader = new BinaryReader(stream, Encoding.ASCII);
        }


        protected override void OnUpdate()
        {
            var layout = Owner.Layout;

            if (layout == null)
            {
                ResetDefaults();
                return;
            }

            CurrentHealth = ReadInt32(layout, CurrentHealthKey);
            MaximumHealth = ReadInt32(layout, MaximumHealthKey);
            CurrentMana = ReadInt32(layout, CurrentManaKey);
            MaximumMana = ReadInt32(layout, MaximumManaKey);
            Level = ReadInt32(layout, LevelKey);
            AbilityLevel = ReadInt32(layout, AbilityLevelKey);
            TotalExperience = ReadInt64(layout, TotalExperienceKey);
            Strength = ReadInt32(layout, StrengthKey);
            Dexterity = ReadInt32(layout, DexterityKey);
            Wisdom = ReadInt32(layout, WisdomKey);
            Constitution = ReadInt32(layout, ConstitutionKey);
            Intelligence = ReadInt32(layout, IntelligenceKey);
            StatPoints = ReadInt32(layout, StatPointsKey);
            ExperienceToNextLevel = ReadInt64(
                layout,
                ExperienceToNextLevelKey);
            GamePoints = ReadInt64(layout, GamePointsKey);
            AbilityToNextLevel = ReadInt64(
                layout,
                AbilityToNextLevelKey);
            TotalAbility = ReadInt64(layout, TotalAbilityKey);
            Weight = ReadInt32(layout, WeightKey);
            MaximumWeight = ReadInt32(layout, MaximumWeightKey);
            ArmorClass = ReadInt32(layout, ArmorClassKey);
            DamageModifier = ReadInt32(layout, DamageModifierKey);
            HitModifier = ReadInt32(layout, HitModifierKey);
            AttackElement = ReadElement(layout, AttackElementKey);
            DefenseElement = ReadElement(layout, DefenseElementKey);
            MagicResistanceUnits = ReadInt32(
                layout,
                MagicResistanceKey);
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
            TotalExperience = 0;
            Strength = 0;
            Dexterity = 0;
            Wisdom = 0;
            Constitution = 0;
            Intelligence = 0;
            StatPoints = 0;
            ExperienceToNextLevel = 0;
            GamePoints = 0;
            AbilityToNextLevel = 0;
            TotalAbility = 0;
            Weight = 0;
            MaximumWeight = 0;
            ArmorClass = 0;
            DamageModifier = 0;
            HitModifier = 0;
            AttackElement = null;
            DefenseElement = null;
            MagicResistanceUnits = 0;
        }

        private int ReadInt32(
            Settings.ClientLayout layout,
            string key)
        {
            var value = ReadInt64(layout, key);
            if (value < int.MinValue || value > int.MaxValue)
                return 0;

            return (int)value;
        }

        private long ReadInt64(
            Settings.ClientLayout layout,
            string key) =>
            ReadInt64(layout, key, out _);

        private long ReadInt64(
            Settings.ClientLayout layout,
            string key,
            out bool wasRead)
        {
            wasRead = false;

            var variable = layout.GetVariable(key);
            if (variable == null || !variable.TryReadInteger(reader, out var value))
                return 0;

            wasRead = true;
            return value;
        }

        private PlayerElement? ReadElement(
            Settings.ClientLayout layout,
            string key)
        {
            var value = ReadInt64(
                layout,
                key,
                out var wasRead);
            if (!wasRead)
                return null;

            return value >= (int)PlayerElement.None && value <= (int)PlayerElement.Undead
                ? (PlayerElement)value
                : null;
        }
    }
}
