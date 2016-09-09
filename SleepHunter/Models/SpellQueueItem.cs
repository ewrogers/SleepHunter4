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
         set { SetProperty(ref id, value, "Id"); }
      }

      public ImageSource Icon
      {
         get { return icon; }
         set { SetProperty(ref icon, value, "Icon"); }
      }

      public string Name
      {
         get { return name; }
         set { SetProperty(ref name, value, "Name"); }
      }

      public SpellTarget Target
      {
         get { return target; }
         set { SetProperty(ref target, value, "Target"); }
      }

      public DateTime LastUsedTimestamp
      {
         get { return lastUsedTimestamp; }
         set { SetProperty(ref lastUsedTimestamp, value, "LastUsedTimestamp"); }
      }

      public int StartingLevel
      {
         get { return startingLevel; }
         set
         {
            SetProperty(ref startingLevel, value, "StartingLevel", onChanged: (s) => { OnPropertyChanged("PercentCompleted"); });
         }
      }

      public int CurrentLevel
      {
         get { return currentLevel; }
         set
         {
            SetProperty(ref currentLevel, value, "CurrentLevel", onChanged: (s) => { OnPropertyChanged("IsDone"); OnPropertyChanged("PercentCompleted"); });
         }
      }

      public int MaximumLevel
      {
         get { return maximumLevel; }
         set
         {
            SetProperty(ref maximumLevel, value, "MaximumLevel", onChanged: (s) => { OnPropertyChanged("IsDone"); OnPropertyChanged("PercentCompleted"); });
         }
      }

      public int? TargetLevel
      {
         get { return targetLevel; }
         set
         {
            SetProperty(ref targetLevel, value, "TargetLevel", onChanged: (s) => { OnPropertyChanged("IsDone"); OnPropertyChanged("HasTargetLevel"); OnPropertyChanged("PercentCompleted"); });
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
         set { SetProperty(ref isUndefined, value, "IsUndefined"); }
      }

      public bool IsActive
      {
         get { return isActive; }
         set { SetProperty(ref isActive, value, "IsActive"); }
      }

      public SpellQueueItem()
      {

      }

      public SpellQueueItem(Spell spellInfo, SavedSpellState spell)
      {
         this.Icon = spellInfo.Icon;
         this.Name = spell.SpellName;
         this.Target = new SpellTarget(spell.TargetMode, new Point(spell.LocationX, spell.LocationY), new Point(spell.OffsetX, spell.OffsetY));
         this.Target.CharacterName = spell.CharacterName;
         this.Target.OuterRadius = spell.OuterRadius;
         this.Target.InnerRadius = spell.InnerRadius;
         this.TargetLevel = spell.TargetLevel > 0 ? spell.TargetLevel : (int?)null;

         this.CurrentLevel = spellInfo.CurrentLevel;
         this.MaximumLevel = spellInfo.MaximumLevel;
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
            other.Id = this.Id;

         other.Icon = this.Icon;
         other.Name = this.Name;
         other.Target = this.Target;
         other.StartingLevel = this.StartingLevel;
         other.CurrentLevel = this.CurrentLevel;
         other.MaximumLevel = this.MaximumLevel;
         other.TargetLevel = this.TargetLevel;
         other.IsUndefined = this.IsUndefined;
         other.IsActive = this.IsActive;
      }

      public override string ToString()
      {
         return string.Format("{0} on {1}",
            name, target.ToString());
      }
   }
}
