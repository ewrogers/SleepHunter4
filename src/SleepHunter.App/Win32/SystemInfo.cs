using System;
using System.Runtime.InteropServices;

namespace SleepHunter.Win32
{
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct SystemInfo
    {
        private readonly uint oemId;
        private readonly uint pageSize;
        private readonly nint minimumApplicationAddress;
        private readonly nint maximumApplicationAddress;
        private readonly nint activeProcessorMask;
        private readonly uint processorType;
        private readonly uint allocationGranularity;
        private readonly ushort processorLevel;
        private readonly ushort processorRevision;

        public readonly uint OemId => oemId;

        public readonly uint PageSize => pageSize;
        public readonly nint MinimumApplicationAddress => minimumApplicationAddress;
        public readonly nint MaximumApplicationAddress => maximumApplicationAddress;

        public readonly nint ActiveProcessorMask => activeProcessorMask;
        public readonly uint ProcessorType => processorType;
        public readonly uint AllocationGranularity => allocationGranularity;
        public readonly ushort ProcessorLevel => processorLevel;
        public readonly ushort ProcessorRevision => processorRevision;
    }
}
