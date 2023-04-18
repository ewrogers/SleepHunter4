
namespace SleepHunter.Models
{
    internal sealed class Skill : Ability
    {
        public Skill()
           : base() { }

        private bool isAssail;
        private bool opensDialog;
        private bool requiresDisarm;

        public bool IsAssail
        {
            get { return isAssail; }
            set { SetProperty(ref isAssail, value, "IsAssail"); }
        }

        public bool OpensDialog
        {
            get { return opensDialog; }
            set { SetProperty(ref opensDialog, value, "OpensDialog"); }
        }

        public bool RequiresDisarm
        {
            get { return requiresDisarm; }
            set { SetProperty(ref requiresDisarm, value, "RequiresDisarm"); }
        }

        public static Skill MakeEmpty(int slot)
        {
            return new Skill
            {
                Slot = slot,
                Panel = Ability.GetSkillPanelForSlot(slot),
                IsEmpty = true
            };
        }

        public override string ToString()
        {
            return string.Format("{0}", this.Name ?? "Unknown Skill");
        }
    }

}
