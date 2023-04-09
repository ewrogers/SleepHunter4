using System;

namespace SleepHunter.Win32
{
    [Flags]
    internal enum VirtualMemoryStatus : uint
    {
        None = 0,
        Commit = 0x1000,
        Free = 0x10000,
        Reserve = 0x2000
    }
}
