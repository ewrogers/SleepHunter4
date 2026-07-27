using System;
using System.Windows.Media;

using CommunityToolkit.Mvvm.ComponentModel;
using SleepHunter.Runtime.Automation;

namespace SleepHunter.ViewModels.Editing
{
    public sealed class SpellQueueItemViewModel :
        ObservableObject
    {
        private long id;
        private ImageSource icon;
        private string name;
        private SpellTargetViewModel target = new();
        private DateTime lastUsedTimestamp;
        private int startingLevel;
        private int currentLevel;
        private int maximumLevel;
        private int? targetLevel;
        private bool isUndefined;
        private bool isActive;
        private bool isOnCooldown;
        private bool isWaitingOnHealth;
        private HealthCondition healthCondition = HealthCondition.Any;

        public long Id
        {
            get => id;
            set => SetProperty(ref id, value);
        }

        public ImageSource Icon
        {
            get => icon;
            set => SetProperty(ref icon, value);
        }

        public string Name
        {
            get => name;
            set => SetProperty(ref name, value);
        }

        public SpellTargetViewModel Target
        {
            get => target;
            set => SetProperty(ref target, value);
        }

        public DateTime LastUsedTimestamp
        {
            get => lastUsedTimestamp;
            set => SetProperty(ref lastUsedTimestamp, value);
        }

        public int StartingLevel
        {
            get => startingLevel;
            set
            {
                if (SetProperty(ref startingLevel, value))
                    OnPropertyChanged(nameof(PercentCompleted));
            }
        }

        public int CurrentLevel
        {
            get => currentLevel;
            set
            {
                if (!SetProperty(ref currentLevel, value))
                    return;

                OnPropertyChanged(nameof(IsDone));
                OnPropertyChanged(nameof(PercentCompleted));
            }
        }

        public int MaximumLevel
        {
            get => maximumLevel;
            set
            {
                if (!SetProperty(ref maximumLevel, value))
                    return;

                OnPropertyChanged(nameof(IsDone));
                OnPropertyChanged(nameof(PercentCompleted));
            }
        }

        public int? TargetLevel
        {
            get => targetLevel;
            set
            {
                if (!SetProperty(ref targetLevel, value))
                    return;

                OnPropertyChanged(nameof(IsDone));
                OnPropertyChanged(nameof(HasTargetLevel));
                OnPropertyChanged(nameof(PercentCompleted));
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

        public bool HasTargetLevel => targetLevel.HasValue;

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
            get => isUndefined;
            set => SetProperty(ref isUndefined, value);
        }

        public bool IsActive
        {
            get => isActive;
            set => SetProperty(ref isActive, value);
        }

        public bool IsOnCooldown
        {
            get => isOnCooldown;
            set => SetProperty(ref isOnCooldown, value);
        }

        public bool IsWaitingOnHealth
        {
            get => isWaitingOnHealth;
            set => SetProperty(ref isWaitingOnHealth, value);
        }

        public HealthCondition HealthCondition
        {
            get => healthCondition;
            set => SetProperty(
                ref healthCondition,
                value ?? HealthCondition.Any);
        }

        public void CopyTo(
            SpellQueueItemViewModel other) =>
            CopyTo(other, true, false);

        public void CopyTo(
            SpellQueueItemViewModel other,
            bool copyId) =>
            CopyTo(other, copyId, false);

        public void CopyTo(
            SpellQueueItemViewModel other,
            bool copyId = true,
            bool copyTimestamp = false)
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
            other.isOnCooldown = IsOnCooldown;
            other.IsWaitingOnHealth = IsWaitingOnHealth;
            other.HealthCondition = HealthCondition;
        }

        public override string ToString() => string.Format("{0} on {1}", name, target.ToString());
    }
}
