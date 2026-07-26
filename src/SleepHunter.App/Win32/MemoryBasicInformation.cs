using System.Runtime.InteropServices;

namespace SleepHunter.Win32
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct MemoryBasicInformation
    {
        private nint baseAddress;
        private nint allocationBase;
        private VirtualMemoryProtection allocationProtect;
        private nint regionSize;
        private VirtualMemoryStatus state;
        private VirtualMemoryProtection protect;
        private VirtualMemoryType type;

        public nint BaseAddress
        {
            readonly get => baseAddress;
            set => baseAddress = value;
        }

        public nint AllocationBase
        {
            readonly get => allocationBase;
            set => allocationBase = value;
        }

        public VirtualMemoryProtection AllocationProtect
        {
            readonly get => allocationProtect;
            set => allocationProtect = value;
        }

        public nint RegionSize
        {
            readonly get => regionSize;
            set => regionSize = value;
        }

        public VirtualMemoryStatus State
        {
            readonly get => state;
            set => state = value;
        }

        public VirtualMemoryProtection Protect
        {
            readonly get => protect;
            set => protect = value;
        }

        public VirtualMemoryType Type
        {
            readonly get => type;
            set => type = value;
        }
    }
}
