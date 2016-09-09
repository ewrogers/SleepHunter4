using System;
using System.Windows;

using SleepHunter.Common;
using SleepHunter.Macro;

namespace SleepHunter.Models
{
    public sealed class FlowerQueueItem : ObservableObject, ICopyable<FlowerQueueItem>
   {
      int id;
      SpellTarget target = new SpellTarget();
      DateTime lastUsedTimestamp = DateTime.Now;
      TimeSpan? interval;
      TimeSpan intervalRemaining;
      int? manaThreshold;

      public int Id
      {
         get { return id; }
         set { SetProperty(ref id, value); }
      }

      public SpellTarget Target
      {
         get { return target; }
         set { SetProperty(ref target, value); }
      }

      public DateTime LastUsedTimestamp
      {
         get { return lastUsedTimestamp; }
         set
         {
            SetProperty(ref lastUsedTimestamp, value, "LastUsedTimestamp");
            Tick();
         }
      }

      public double IntervalSeconds
      {
         get { return interval.HasValue ? interval.Value.TotalSeconds : 0; }
      }

      public TimeSpan? Interval
      {
         get { return interval; }
         set
         {
            var originalTime = interval.HasValue ? interval.Value : TimeSpan.Zero;
            var newTime = value.HasValue ? value.Value : TimeSpan.Zero;

            var deltaTime = originalTime - newTime;

            SetProperty(ref interval, value, "Interval", onChanged: (s) => { OnPropertyChanged("IntervalSeconds"); Tick(deltaTime); });
         }
      }

      public TimeSpan ElapsedTime
      {
         get
         {
            if (interval.HasValue)
               return interval.Value - intervalRemaining;
            else
               return TimeSpan.Zero;
         }
      }

      public double ElapsedTimeSeconds
      {
         get { return this.ElapsedTime.TotalSeconds; }
      }

      public TimeSpan RemainingTime
      {
         get
         {
            if (!interval.HasValue)
               return TimeSpan.Zero;

            var elapsed = this.ElapsedTime;
            var remaining = interval.Value - elapsed;

            if (remaining <= TimeSpan.Zero)
               return TimeSpan.Zero;

            return remaining;
         }
      }

      public double RemainingTimeSeconds
      {
         get { return this.RemainingTime.TotalSeconds; }
      }

      public bool IsReady
      {
         get
         {
            if (interval.HasValue)
               return this.RemainingTime <= TimeSpan.Zero;
            else
               return false;
         }
      }

      public int? ManaThreshold
      {
         get { return manaThreshold; }
         set { SetProperty(ref manaThreshold, value, "ManaThreshold"); }
      }

      public FlowerQueueItem()
      {

      }

      public FlowerQueueItem(SavedFlowerState flower)
      {
         this.Target = new SpellTarget(flower.TargetMode, new Point(flower.LocationX, flower.LocationY), new Point(flower.OffsetX, flower.OffsetY));
         this.Target.CharacterName = flower.CharacterName;
         this.Target.OuterRadius = flower.OuterRadius;
         this.Target.InnerRadius = flower.InnerRadius;

         this.Interval = flower.HasInterval ? flower.Interval : (TimeSpan?)null;
         this.ManaThreshold = flower.ManaThreshold > 0 ? flower.ManaThreshold : (int?)null;
      }

      public void ResetTimer()
      {
         if (interval.HasValue)
            intervalRemaining = interval.Value;
         else
            intervalRemaining = TimeSpan.Zero;
      }

      public void Tick()
      {
         Tick(TimeSpan.Zero);
      }

      public void Tick(TimeSpan deltaTime)
      {
         intervalRemaining -= deltaTime;
         
         OnPropertyChanged("ElapsedTime");
         OnPropertyChanged("ElapsedTimeSeconds");
         OnPropertyChanged("RemainingTime");
         OnPropertyChanged("RemainingTimeSeconds");
         OnPropertyChanged("IsReady");
      }

      public void CopyTo(FlowerQueueItem other)
      {
         CopyTo(other, true);
      }

      public void CopyTo(FlowerQueueItem other, bool copyId)
      {
         CopyTo(other, copyId, false);
      }

      public void CopyTo(FlowerQueueItem other, bool copyId = true, bool copyTimestamp = false)
      {
         if (copyId)
            other.Id = this.Id;

         other.Target = this.Target;
         other.Interval = this.Interval;
         other.ManaThreshold = this.ManaThreshold;

         if (copyTimestamp)
            other.LastUsedTimestamp = this.LastUsedTimestamp;
      }

      public override string ToString()
      {
         return string.Format("Flowering on {0}", target.ToString());
      }
   }
}
