using System;

using SleepHunter.Common;

namespace SleepHunter.Models
{
    public sealed class FlowerQueueItem : ObservableObject, ICopyable<FlowerQueueItem>
    {
        private int id;
        private SpellTarget target = new();
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

                SetProperty(ref interval, value, onChanged: (s) => { RaisePropertyChanged(nameof(IntervalSeconds)); Tick(deltaTime); });
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

        public bool IsReady
        {
            get
            {
                if (interval.HasValue)
                    return RemainingTime <= TimeSpan.Zero;
                else
                    return false;
            }
        }

        public int? ManaThreshold
        {
            get => manaThreshold;
            set => SetProperty(ref manaThreshold, value);
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

        public void CopyTo(FlowerQueueItem other) => CopyTo(other, true);

        public void CopyTo(FlowerQueueItem other, bool copyId) => CopyTo(other, copyId, false);

        public void CopyTo(FlowerQueueItem other, bool copyId = true, bool copyTimestamp = false)
        {
            if (copyId)
                other.Id = Id;

            other.Target = Target;
            other.Interval = Interval;
            other.ManaThreshold = ManaThreshold;

            if (copyTimestamp)
                other.LastUsedTimestamp = LastUsedTimestamp;
        }

        public override string ToString() => string.Format("Flowering on {0}", target.ToString());
    }
}
