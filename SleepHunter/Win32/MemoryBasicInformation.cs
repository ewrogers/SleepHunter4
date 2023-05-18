using System;
using System.Runtime.InteropServices;

namespace SleepHunter.Win32
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct MemoryBasicInformation
    {
        IntPtr baseAddress;
        IntPtr allocationBase;
        VirtualMemoryProtection allocationProtect;
        IntPtr regionSize;
        VirtualMemoryStatus state;
        VirtualMemoryProtection protect;
        VirtualMemoryType type;

        public IntPtr BaseAddress
        {
            get { return baseAddress; }
            set { baseAddress = value; }
        }

        public IntPtr AllocationBase
        {
            get { return allocationBase; }
            set { allocationBase = value; }
        }

        public VirtualMemoryProtection AllocationProtect
        {
            get { return allocationProtect; }
            set { allocationProtect = value; }
        }

        public IntPtr RegionSize
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
