using System;
using System.Windows;
using System.Windows.Media;

using SleepHunter.Common;
using SleepHunter.Macro;

namespace SleepHunter.Models
{
  public sealed class SpellQueueItem : ObservableObject, ICopyable<SpellQueueItem>
  {
    int id;
    ImageSource icon;
    string name;
    SpellTarget target = new SpellTarget();
    DateTime lastUsedTimestamp;
    int startingLevel;
    int currentLevel;
    int maximumLevel;
    int? targetLevel;
    bool isUndefined;
    bool isActive;

    public int Id
    {
      get { return id; }
      set { SetProperty(ref id, value); }
    }

    public ImageSource Icon
    {
      get { return icon; }
      set { SetProperty(ref icon, value); }
    }

    public string Name
    {
      get { return name; }
      set { SetProperty(ref name, value); }
    }

    public SpellTarget Target
    {
      get { return target; }
      set { SetProperty(ref target, value); }
    }

    public DateTime LastUsedTimestamp
    {
      get { return lastUsedTimestamp; }
      set { SetProperty(ref lastUsedTimestamp, value); }
    }

    public int StartingLevel
    {
      get { return startingLevel; }
      set
      {
        SetProperty(ref startingLevel, value, onChanged: (s) => { RaisePropertyChanged("PercentCompleted"); });
      }
    }

    public int CurrentLevel
    {
      get { return currentLevel; }
      set
      {
        SetProperty(ref currentLevel, value, onChanged: (s) => { RaisePropertyChanged("IsDone"); RaisePropertyChanged("PercentCompleted"); });
      }
    }

    public int MaximumLevel
    {
      get { return maximumLevel; }
      set
      {
        SetProperty(ref maximumLevel, value, onChanged: (s) => { RaisePropertyChanged("IsDone"); RaisePropertyChanged("PercentCompleted"); });
      }
    }

    public int? TargetLevel
    {
      get { return targetLevel; }
      set
      {
        SetProperty(ref targetLevel, value, onChanged: (s) => { RaisePropertyChanged("IsDone"); RaisePropertyChanged("HasTargetLevel"); RaisePropertyChanged("PercentCompleted"); });
      }
    }

    public double PercentCompleted
    {
      get
      {
        if (!HasTargetLevel || CurrentLevel >= TargetLevel.Value)
          return 100;

        return currentLevel * 100.0 / targetLevel.Value;
      }
    }

    public bool HasTargetLevel
    {
      get { return targetLevel.HasValue; }
    }

    public bool IsDone
    {
      get
      {
        if (!targetLevel.HasValue)
          return false;

        return currentLevel >= targetLevel.Value;
      }
    }

    public bool IsUndefined
    {
      get { return isUndefined; }
      set { SetProperty(ref isUndefined, value); }
    }

    public bool IsActive
    {
      get { return isActive; }
      set { SetProperty(ref isActive, value); }
    }

    public SpellQueueItem() { }

    public SpellQueueItem(Spell spellInfo, SavedSpellState spell)
    {
      Icon = spellInfo.Icon;
      Name = spell.SpellName;
      Target = new SpellTarget(spell.TargetMode, new Point(spell.LocationX, spell.LocationY), new Point(spell.OffsetX, spell.OffsetY));
      Target.CharacterName = spell.CharacterName;
      Target.OuterRadius = spell.OuterRadius;
      Target.InnerRadius = spell.InnerRadius;
      TargetLevel = spell.TargetLevel > 0 ? spell.TargetLevel : (int?)null;

      CurrentLevel = spellInfo.CurrentLevel;
      MaximumLevel = spellInfo.MaximumLevel;
    }

    public void CopyTo(SpellQueueItem other)
    {
      CopyTo(other, true, false);
    }

    public void CopyTo(SpellQueueItem other, bool copyId)
    {
      CopyTo(other, copyId, false);
    }

    public void CopyTo(SpellQueueItem other, bool copyId = true, bool copyTimestamp = false)
    {
      if (copyId)
        other.Id = Id;

      other.Icon = Icon;
      other.Name = Name;
      other.Target = Target;
      other.StartingLevel = StartingLevel;
      other.CurrentLevel = CurrentLevel;
      other.MaximumLevel = MaximumLevel;
      other.TargetLevel = TargetLevel;
      other.IsUndefined = IsUndefined;
      other.IsActive = IsActive;
    }

    public override string ToString()
    {
      return string.Format("{0} on {1}", name, target.ToString());
    }
  }
}
