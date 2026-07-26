using System;

namespace SleepHunter.Win32
{
    [Flags]
    internal enum VirtualMemoryAllocationType : uint
    {
        Commit = 0x1000,
        Reserve = 0x2000
    }
}
