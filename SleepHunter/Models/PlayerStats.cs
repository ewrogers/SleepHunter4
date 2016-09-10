using System;
using System.Diagnostics;
using System.IO;
using System.Text;

using SleepHunter.Common;
using SleepHunter.IO.Process;

namespace SleepHunter.Models
{
  public sealed class PlayerStats : ObservableObject
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

      long currentHealth, maximumHealth;
      long currentMana, maximumMana;
      long level, abilityLevel;

      Debug.WriteLine($"Updating stats (pid={accessor.ProcessId})...");

      Stream stream = null;
      try
      {
        stream = accessor.GetStream();
        using (var reader = new BinaryReader(stream, Encoding.ASCII))
        {
          stream = null;

          // Current Health
          if (currentHealthVariable != null && currentHealthVariable.TryReadIntegerString(reader, out currentHealth))
            CurrentHealth = (int)currentHealth;
          else
            CurrentHealth = 0;

          // Max Health
          if (maximumHealthVariable != null && maximumHealthVariable.TryReadIntegerString(reader, out maximumHealth))
            MaximumHealth = (int)maximumHealth;
          else
            MaximumHealth = 0;

          // Current Mana
          if (currentManaVariable != null && currentManaVariable.TryReadIntegerString(reader, out currentMana))
            CurrentMana = (int)currentMana;
          else
            CurrentMana = 0;

          // Max Mana
          if (maximumManaVariable != null && maximumManaVariable.TryReadIntegerString(reader, out maximumMana))
            MaximumMana = (int)maximumMana;
          else
            MaximumMana = 0;

          // Level
          if (levelVariable != null && levelVariable.TryReadIntegerString(reader, out level))
            Level = (int)level;
          else
            Level = 0;

          // Ability Level
          if (abilityLevelVariable != null && abilityLevelVariable.TryReadIntegerString(reader, out abilityLevel))
            AbilityLevel = (int)abilityLevel;
          else
            AbilityLevel = 0;
        }
      }
      finally { stream?.Dispose(); }

      Debug.WriteLine($"CurrentHealth = {CurrentHealth}");
      Debug.WriteLine($"MaximumHealth = {MaximumHealth}");
      Debug.WriteLine($"CurrentMana = {CurrentMana}");
      Debug.WriteLine($"MaximumMana = {MaximumMana}");
      Debug.WriteLine($"Level = { Level}");
      Debug.WriteLine($"AbilityLevel = {AbilityLevel}");
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
