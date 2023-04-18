
namespace SleepHunter.Models
{
    public sealed class Spell : Ability
    {
        public static readonly string LyliacPlantKey = "Lyliac Plant";
        public static readonly string LyliacVineyardKey = "Lyliac Vineyard";
        public static readonly string FasSpioradKey = "Fas Spiorad";

        private SpellTargetMode targetMode;
        private string prompt;

        public SpellTargetMode TargetMode
        {
            get => targetMode;
            set => SetProperty(ref targetMode, value);
        }

        public string Prompt
        {
            get => prompt;
            set => SetProperty(ref prompt, value);
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
