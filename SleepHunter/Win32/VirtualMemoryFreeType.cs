using System;

namespace SleepHunter.Win32
{
    [Flags]
    internal enum VirtualMemoryFreeType : uint
    {
        Decommit = 0x4000,
        Release = 0x8000
    }
}
