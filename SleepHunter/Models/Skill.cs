
namespace SleepHunter.Models
{
    public sealed class Skill : Ability
    {
        private bool isAssail;
        private bool opensDialog;
        private bool requiresDisarm;

        public bool IsAssail
        {
            get { return isAssail; }
            set { SetProperty(ref isAssail, value); }
        }

        public bool OpensDialog
        {
            get { return opensDialog; }
            set { SetProperty(ref opensDialog, value); }
        }

        public bool RequiresDisarm
        {
            get { return requiresDisarm; }
            set { SetProperty(ref requiresDisarm, value); }
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
