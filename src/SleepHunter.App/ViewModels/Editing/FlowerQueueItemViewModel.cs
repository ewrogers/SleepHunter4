using System;

using CommunityToolkit.Mvvm.ComponentModel;

namespace SleepHunter.ViewModels.Editing
{
    public sealed class FlowerQueueItemViewModel :
        ObservableObject
    {
        private long id;
        private SpellTargetViewModel target = new();
        private DateTime lastUsedTimestamp = DateTime.Now;
        private TimeSpan? interval;
        private TimeSpan intervalRemaining;
        private int? manaThreshold;

        public long Id
        {
            get => id;
            set => SetProperty(ref id, value);
        }

        public SpellTargetViewModel Target
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

                if (!SetProperty(ref interval, value))
                    return;

                OnPropertyChanged(nameof(IntervalSeconds));
                Tick(deltaTime);
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

        public void Tick() => Tick(TimeSpan.Zero);

        public void Tick(TimeSpan deltaTime)
        {
            intervalRemaining -= deltaTime;

            RaiseTimerPropertiesChanged();
        }

        public void UpdateRemainingTime(TimeSpan remainingTime)
        {
            var normalizedTime = remainingTime > TimeSpan.Zero
                ? remainingTime
                : TimeSpan.Zero;
            if (intervalRemaining == normalizedTime)
                return;

            intervalRemaining = normalizedTime;
            RaiseTimerPropertiesChanged();
        }

        public void ResetTimer() =>
            UpdateRemainingTime(interval ?? TimeSpan.Zero);

        private void RaiseTimerPropertiesChanged()
        {
            OnPropertyChanged(nameof(ElapsedTime));
            OnPropertyChanged(nameof(ElapsedTimeSeconds));
            OnPropertyChanged(nameof(RemainingTime));
            OnPropertyChanged(nameof(RemainingTimeSeconds));
            OnPropertyChanged(nameof(IsReady));
        }

        public void CopyTo(
            FlowerQueueItemViewModel other) =>
            CopyTo(other, true);

        public void CopyTo(
            FlowerQueueItemViewModel other,
            bool copyId) =>
            CopyTo(other, copyId, false);

        public void CopyTo(
            FlowerQueueItemViewModel other,
            bool copyId = true,
            bool copyTimestamp = false)
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
