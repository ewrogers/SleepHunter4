using System.Windows;

namespace SleepHunter.Models
{
    public enum InterfacePanel : byte
    {
        Inventory = 0,
        TemuairSpells = 1,
        MedeniaSpells = 2,
        TemuairSkills = 3,
        MedeniaSkills = 4,
        Chat = 5,
        ChatHistory = 6,
        Stats = 7,
        Modifiers = 8,
        WorldSkills = 9,
        WorldSpells = 10,
        Unknown = 0xFF
    }

    
}
