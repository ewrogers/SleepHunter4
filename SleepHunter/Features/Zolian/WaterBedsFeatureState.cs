using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SleepHunter.Features.Zolian
{
    // Represents the 'UseWaterAndBeds' feature flag state (Zolian)
    // This feature allows the user to interact with water/beds in the environment to restore MP
    public sealed class WaterBedsFeatureState : FeatureState
    {
        private bool enabled;

        public bool IsEnabled
        {
            get => enabled;
            set => SetProperty(ref enabled, value);
        }

        public override void ResetToDefault()
        {
            base.ResetToDefault();
            IsEnabled = false;
        }
    }
}
