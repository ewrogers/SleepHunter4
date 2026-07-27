
namespace SleepHunter.ViewModels.Presentation
{
    public sealed class SkillViewModel : AbilityViewModel
    {
        private bool opensDialog;
        private double? minHealthPercent;
        private double? maxHealthPercent;
        private uint cooldownProgress;

        public bool OpensDialog
        {
            get => opensDialog;
            set => SetProperty(ref opensDialog, value);
        }

        public double? MinHealthPercent
        {
            get => minHealthPercent;
            set => SetProperty(ref minHealthPercent, value);
        }

        public double? MaxHealthPercent
        {
            get => maxHealthPercent;
            set => SetProperty(ref maxHealthPercent, value);
        }

        public uint CooldownProgress
        {
            get => cooldownProgress;
            set
            {
                if (!SetProperty(ref cooldownProgress, value))
                    return;

                CooldownRemainingFraction =
                    1.0 -
                    System.Math.Clamp(
                        CooldownProgress / 30.0,
                        0.0,
                        1.0);
            }
        }

        public static SkillViewModel MakeEmpty(int slot)
        {
            return new SkillViewModel
            {
                Slot = slot,
                Panel = GetSkillPanelForSlot(slot),
                IsEmpty = true
            };
        }

        public override string ToString() => Name ?? "Unknown Skill";
    }

}
