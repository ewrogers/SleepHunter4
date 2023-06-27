
namespace SleepHunter.Models
{
    public sealed class Skill : Ability
    {
        private bool isAssail;
        private bool opensDialog;
        private bool requiresDisarm;
        private double? minHealthPercent;
        private double? maxHealthPercent;

        public bool IsAssail
        {
            get => isAssail;
            set => SetProperty(ref isAssail, value);
        }

        public bool OpensDialog
        {
            get => opensDialog;
            set => SetProperty(ref opensDialog, value);
        }

        public bool RequiresDisarm
        {
            get => requiresDisarm;
            set => SetProperty(ref requiresDisarm, value);
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
