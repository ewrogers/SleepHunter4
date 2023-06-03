using System;
using System.Runtime.InteropServices;

namespace SleepHunter.Win32
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct MemoryBasicInformation
    {
        nint baseAddress;
        nint allocationBase;
        VirtualMemoryProtection allocationProtect;
        nint regionSize;
        VirtualMemoryStatus state;
        VirtualMemoryProtection protect;
        VirtualMemoryType type;

        public nint BaseAddress
        {
            get { return baseAddress; }
            set { baseAddress = value; }
        }

        public nint AllocationBase
        {
            get { return allocationBase; }
            set { allocationBase = value; }
        }

        public VirtualMemoryProtection AllocationProtect
        {
            get { return allocationProtect; }
            set { allocationProtect = value; }
        }

        public nint RegionSize
        {
            get { return regionSize; }
            set { regionSize = value; }
        }

        public VirtualMemoryStatus State
        {
            get { return state; }
            set { state = value; }
        }

        public VirtualMemoryProtection Protect
        {
            get { return protect; }
            set { protect = value; }
        }

        public VirtualMemoryType Type
        {
            get { return type; }
            set { type = value; }
        }
    }
}
