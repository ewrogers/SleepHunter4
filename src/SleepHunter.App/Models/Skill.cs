
namespace SleepHunter.Models
{
    public sealed class Skill : Ability
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
            set => SetProperty(ref cooldownProgress, value,
                onChanged: (_) =>
                {
                    CooldownRemainingFraction = 1.0 - System.Math.Clamp(CooldownProgress / 30.0, 0.0, 1.0);
                });
        }

        public static Skill MakeEmpty(int slot)
        {
            return new Skill
            {
                Slot = slot,
                Panel = GetSkillPanelForSlot(slot),
                IsEmpty = true
            };
        }

        public override string ToString() => Name ?? "Unknown Skill";
    }

}
