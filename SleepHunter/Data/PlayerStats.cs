using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using SleepHunter.IO;
using SleepHunter.IO.Process;

namespace SleepHunter.Data
{
   public sealed class PlayerStats : NotifyObject
   {
      static readonly string CurrentHealthKey = @"CurrentHealth";
      static readonly string MaximumHealthKey = @"MaximumHealth";
      static readonly string CurrentManaKey = @"CurrentMana";
      static readonly string MaximumManaKey = @"MaximumMana";
      static readonly string LevelKey = @"Level";
      static readonly string AbilityLevelKey = @"AbilityLevel";

      Player owner;
      int currentHealth;
      int maximumHealth;
      int currentMana;
      int maximumMana;
      int level;
      int abilityLevel;

      public Player Owner
      {
         get { return owner; }
         set { SetProperty(ref owner, value, "Owner"); }
      }

      public int CurrentHealth
      {
         get { return currentHealth; }
         set
         {
            SetProperty(ref currentHealth, value, "CurrentHealth", onChanged: (s) => { OnPropertyChanged("HealthPercent"); OnPropertyChanged("HasFullHealth"); });
         }
      }

      public int MaximumHealth
      {
         get { return maximumHealth; }
         set
         {
            SetProperty(ref maximumHealth, value, "MaximumHealth", onChanged: (s) => { OnPropertyChanged("HealthPercent"); OnPropertyChanged("HasFullHealth"); });
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
            SetProperty(ref currentMana, value, "CurrentMana", onChanged: (s) => { OnPropertyChanged("ManaPercent"); OnPropertyChanged("HasFullMana"); });
         }
      }

      public int MaximumMana
      {
         get { return maximumMana; }
         set
         {
            SetProperty(ref maximumMana, value, "MaximumMana", onChanged: (s) => { OnPropertyChanged("ManaPercent"); OnPropertyChanged("HasFullMana"); });
         }
      }

      public bool HasFullMana
      {
         get { return currentMana >= maximumMana && currentMana> 0; }
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
         set { SetProperty(ref level, value, "Level"); }
      }

      public int AbilityLevel
      {
         get { return abilityLevel; }
         set { SetProperty(ref abilityLevel, value, "AbilityLevel"); }
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

         var version = this.Owner.Version;

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

         long currentHealth, maximumHealth;
         long currentMana, maximumMana;
         long level, abilityLevel;
                  
         using(var stream = accessor.GetStream())
         using (var reader = new BinaryReader(stream, Encoding.ASCII))
         {
            // Current Health
            if (currentHealthVariable != null && currentHealthVariable.TryReadIntegerString(reader, out currentHealth))
               this.CurrentHealth = (int)currentHealth;
            else
               this.CurrentHealth = 0;

            // Max Health
            if (maximumHealthVariable != null && maximumHealthVariable.TryReadIntegerString(reader, out maximumHealth))
               this.MaximumHealth = (int)maximumHealth;
            else
               this.MaximumHealth = 0;

            // Current Mana
            if (currentManaVariable != null && currentManaVariable.TryReadIntegerString(reader, out currentMana))
               this.CurrentMana = (int)currentMana;
            else
               this.CurrentMana = 0;

            // Max Mana
            if (maximumManaVariable != null && maximumManaVariable.TryReadIntegerString(reader, out maximumMana))
               this.MaximumMana = (int)maximumMana;
            else
               this.MaximumMana = 0;

            // Level
            if (levelVariable != null && levelVariable.TryReadIntegerString(reader, out level))
               this.Level = (int)level;
            else
               this.Level = 0;

            // Ability Level
            if (abilityLevelVariable != null && abilityLevelVariable.TryReadIntegerString(reader, out abilityLevel))
               this.AbilityLevel = (int)abilityLevel;
            else
               this.AbilityLevel = 0;
         }
      }

      public void ResetDefaults()
      {
         this.CurrentHealth = 0;
         this.MaximumHealth = 0;
         this.CurrentMana = 0;
         this.MaximumMana = 0;
         this.Level = 0;
         this.AbilityLevel = 0;
      }
   }
}
