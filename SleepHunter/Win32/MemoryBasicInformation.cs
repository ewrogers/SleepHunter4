using System;
using System.Runtime.InteropServices;

namespace SleepHunter.Win32
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct MemoryBasicInformation
    {
        private IntPtr baseAddress;
        private IntPtr allocationBase;
        private VirtualMemoryProtection allocationProtect;
        private uint regionSize;
        private VirtualMemoryStatus state;
        private VirtualMemoryProtection protect;
        private VirtualMemoryType type;

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

        public uint RegionSize
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
