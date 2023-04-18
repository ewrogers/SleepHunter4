using System;

namespace SleepHunter.Models
{
    [Flags]
    public enum PlayerFieldFlags : uint
    {
        None = 0x0,
        Name = 0x1,
        Guild = 0x2,
        GuildRank = 0x4,
        Title = 0x8,
        Inventory = 0x10,
        Equipment = 0x20,
        Skillbook = 0x40,
        Spellbook = 0x80,
        Stats = 0x100,
        Modifiers = 0x200,
        Location = 0x400,
        GameClient = 0x800,
        Status = 0x1000,
        Window = 0x2000,

        All = 0xFFFFFFFF
    }
}
