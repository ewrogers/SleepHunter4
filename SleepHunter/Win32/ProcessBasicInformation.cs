using System.Runtime.InteropServices;

namespace SleepHunter.Win32
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct ProcessBasicInformation
    {
        public int ExitStatus;
        public nint PebBaseAddress;
        public nuint AffinityMask;
        public int BasePriority;
        public nuint UniqueProcessId;
        public nuint InheritedFromUniqueProcessId;
    }
}
