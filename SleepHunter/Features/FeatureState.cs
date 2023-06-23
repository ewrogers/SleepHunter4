using SleepHunter.Common;

namespace SleepHunter.Features
{
    public abstract class FeatureState : ObservableObject
    {
        public FeatureState() => ResetToDefault();

        public virtual void ResetToDefault() { }
    }
}
