using System;
using System.Runtime.InteropServices;

namespace SleepHunter.Win32
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct SystemInfo
    {
        uint oemId;
        uint pageSize;
        nint minimumApplicationAddress;
        nint maximumApplicationAddress;
        nint activeProcessorMask;
        uint processorType;
        uint allocationGranularity;
        ushort processorLevel;
        ushort processorRevision;

        public uint OemId => oemId;

        public uint PageSize => pageSize;
        public nint MinimumApplicationAddress => minimumApplicationAddress;
        public nint MaximumApplicationAddress => maximumApplicationAddress;

        public nint ActiveProcessorMask => activeProcessorMask;
        public uint ProcessorType => processorType;
        public uint AllocationGranularity => allocationGranularity;
        public ushort ProcessorLevel => processorLevel;
        public ushort ProcessorRevision => processorRevision;
    }
}
