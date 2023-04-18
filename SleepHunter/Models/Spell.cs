
namespace SleepHunter.Models
{
    internal enum SpellTargetMode : byte
    {
        None = 5,
        Target = 2,
        TextInput = 1
    }

    internal sealed class Spell : Ability
    {
        public static readonly string LyliacPlantKey = "Lyliac Plant";
        public static readonly string LyliacVineyardKey = "Lyliac Vineyard";
        public static readonly string FasSpioradKey = "Fas Spiorad";

        SpellTargetMode targetMode;
        string prompt;

        public Spell()
           : base() { }

        public SpellTargetMode TargetMode
        {
            get { return targetMode; }
            set { SetProperty(ref targetMode, value, "TargetMode"); }
        }

        public string Prompt
        {
            get { return prompt; }
            set { SetProperty(ref prompt, value, "Prompt"); }
        }

        public static Spell MakeEmpty(int slot)
        {
            return new Spell
            {
                Slot = slot,
                Panel = Ability.GetSpellPanelForSlot(slot),
                IsEmpty = true
            };
        }

        public override string ToString()
        {
            return string.Format("{0}", this.Name ?? "Unknown Spell");
        }
    }
}
