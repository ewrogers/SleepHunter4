
namespace SleepHunter.Models
{
    public sealed class Spell : Ability
    {
        public const string LyliacPlantKey = @"Lyliac Plant";
        public const string LyliacVineyardKey = @"Lyliac Vineyard";
        public const string FasSpioradKey = @"Fas Spiorad";

        private AbilityTargetType targetType;
        private string prompt;

        public AbilityTargetType TargetType
        {
            get { return targetType; }
            set { SetProperty(ref targetType, value); }
        }

        public string Prompt
        {
            get { return prompt; }
            set { SetProperty(ref prompt, value); }
        }

        public static Spell MakeEmpty(int slot)
        {
            return new Spell
            {
                Slot = slot,
                Panel = GetSpellPanelForSlot(slot),
                IsEmpty = true
            };
        }

        public override string ToString() => Name ?? "Unknown Spell";
    }
}
