using System;
using System.Windows;

using SleepHunter.Common;
using SleepHunter.Macro;

namespace SleepHunter.Models
{
    public sealed class FlowerQueueItem : ObservableObject
    {
        private int id;
        private SpellTarget target = new SpellTarget();
        private DateTime lastUsedTimestamp = DateTime.Now;
        private TimeSpan? interval;
        private TimeSpan intervalRemaining;
        private int? manaThreshold;

        public int Id
        {
            get => id;
            set => SetProperty(ref id, value);
        }

        public SpellTarget Target
        {
            get => target;
            set => SetProperty(ref target, value);
        }

        public DateTime LastUsedTimestamp
        {
            get => lastUsedTimestamp;
            set
            {
                SetProperty(ref lastUsedTimestamp, value);
                Tick();
            }
        }

        public double IntervalSeconds => interval.HasValue ? interval.Value.TotalSeconds : 0;

        public TimeSpan? Interval
        {
            get => interval;
            set
            {
                var originalTime = interval ?? TimeSpan.Zero;
                var newTime = value ?? TimeSpan.Zero;

                var deltaTime = originalTime - newTime;

                SetProperty(ref interval, value, onChanged: (s) => 
                {
                    RaisePropertyChanged(nameof(IntervalSeconds));
                    Tick(deltaTime);
                });
            }
        }

        public TimeSpan ElapsedTime => interval.HasValue ? interval.Value - intervalRemaining : TimeSpan.Zero;

        public double ElapsedTimeSeconds => ElapsedTime.TotalSeconds;

        public TimeSpan RemainingTime
        {
            get
            {
                if (!interval.HasValue)
                    return TimeSpan.Zero;

                var elapsed = ElapsedTime;
                var remaining = interval.Value - elapsed;

                if (remaining <= TimeSpan.Zero)
                    return TimeSpan.Zero;

                return remaining;
            }
        }

        public double RemainingTimeSeconds => RemainingTime.TotalSeconds;

        public bool IsReady => interval.HasValue ? RemainingTime <= TimeSpan.Zero : false;

        public int? ManaThreshold
        {
            get => manaThreshold;
            set => SetProperty(ref manaThreshold, value);
        }

        public FlowerQueueItem() { }

        public FlowerQueueItem(SavedFlowerState flower)
        {
            Target = new SpellTarget(flower.TargetMode, new Point(flower.LocationX, flower.LocationY), new Point(flower.OffsetX, flower.OffsetY))
            {
                CharacterName = flower.CharacterName,
                OuterRadius = flower.OuterRadius,
                InnerRadius = flower.InnerRadius
            };

            Interval = flower.HasInterval ? flower.Interval : (TimeSpan?)null;
            ManaThreshold = flower.ManaThreshold > 0 ? flower.ManaThreshold : (int?)null;
        }

        public void ResetTimer()
        {
            if (interval.HasValue)
                intervalRemaining = interval.Value;
            else
                intervalRemaining = TimeSpan.Zero;
        }

        public void Tick() => Tick(TimeSpan.Zero);

        public void Tick(TimeSpan deltaTime)
        {
            intervalRemaining -= deltaTime;

            RaisePropertyChanged(nameof(ElapsedTime));
            RaisePropertyChanged(nameof(ElapsedTimeSeconds));
            RaisePropertyChanged(nameof(RemainingTime));
            RaisePropertyChanged(nameof(RemainingTimeSeconds));
            RaisePropertyChanged(nameof(IsReady));
        }

        public void CopyTo(FlowerQueueItem other, bool copyTimestamp = false)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            other.Id = Id;
            other.Target = Target;
            other.Interval = Interval;
            other.ManaThreshold = ManaThreshold;

            if (copyTimestamp)
                other.LastUsedTimestamp = LastUsedTimestamp;
        }

        public override string ToString() => $"Flowering on {target}";
    }
}
