using System;

namespace SleepHunter.Models
{
    [Flags]
    public enum PlayerClass
    {
        Peasant = 0,
        Warrior = 0x1,
        Wizard = 0x2,
        Priest = 0x4,
        Rogue = 0x8,
        Monk = 0x10,
        All = Warrior | Wizard | Priest | Rogue | Monk
    }
}
