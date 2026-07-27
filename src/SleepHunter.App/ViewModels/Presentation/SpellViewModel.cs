
using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.ViewModels.Presentation
{
    public sealed class SpellViewModel : AbilityViewModel
    {
        public const string LyliacPlantKey = @"Lyliac Plant";
        public const string LyliacVineyardKey = @"Lyliac Vineyard";
        private SpellArgumentType argumentType;
        private bool opensDialog;
        private double? minHealthPercent;
        private double? maxHealthPercent;

        public SpellArgumentType ArgumentType
        {
            get => argumentType;
            set => SetProperty(ref argumentType, value);
        }

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

        public static SpellViewModel MakeEmpty(int slot)
        {
            return new SpellViewModel
            {
                Slot = slot,
                Panel = GetSpellPanelForSlot(slot),
                IsEmpty = true
            };
        }

        public override string ToString() => Name ?? "Unknown Spell";
    }
}
