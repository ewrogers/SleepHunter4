
namespace SleepHunter.Models
{
    public sealed class Spell : Ability
    {
        public const string LyliacPlantKey = @"Lyliac Plant";
        public const string LyliacVineyardKey = @"Lyliac Vineyard";
        public const string FasSpioradKey = @"Fas Spiorad";

        private SpellTargetMode targetMode;
        private string prompt;

        public SpellTargetMode TargetMode
        {
            get { return targetMode; }
            set { SetProperty(ref targetMode, value); }
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
