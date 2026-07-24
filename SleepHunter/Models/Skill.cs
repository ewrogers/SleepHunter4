
namespace SleepHunter.Models
{
    public sealed class Skill : Ability
    {
        private bool isAssail;
        private bool opensDialog;
        private bool requiresDisarm;
        private double? minHealthPercent;
        private double? maxHealthPercent;
        private uint cooldownProgress;
        private uint cooldownStartMilliseconds;
        private uint cooldownEndMilliseconds;

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

        public uint CooldownProgress
        {
            get => cooldownProgress;
            set => SetProperty(ref cooldownProgress, value,
                onChanged: (_) => RaisePropertyChanged(nameof(CooldownProgressPercent)));
        }

        public double CooldownProgressPercent => System.Math.Clamp(CooldownProgress / 30.0, 0.0, 1.0);

        public uint CooldownStartMilliseconds
        {
            get => cooldownStartMilliseconds;
            set => SetProperty(ref cooldownStartMilliseconds, value,
                onChanged: (_) => RaisePropertyChanged(nameof(CooldownDurationMilliseconds)));
        }

        public uint CooldownEndMilliseconds
        {
            get => cooldownEndMilliseconds;
            set => SetProperty(ref cooldownEndMilliseconds, value,
                onChanged: (_) => RaisePropertyChanged(nameof(CooldownDurationMilliseconds)));
        }

        public uint CooldownDurationMilliseconds =>
            unchecked(CooldownEndMilliseconds - CooldownStartMilliseconds);

        public uint GetRemainingCooldownMilliseconds(uint currentMilliseconds)
        {
            if (!IsOnCooldown)
                return 0;

            var duration = CooldownDurationMilliseconds;
            var elapsed = unchecked(currentMilliseconds - CooldownStartMilliseconds);
            return elapsed >= duration ? 0 : duration - elapsed;
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
