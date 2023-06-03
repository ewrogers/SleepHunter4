using System.Runtime.InteropServices;

namespace SleepHunter.Win32
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct ProcessInformation
    {
        private nint processHandle;
        private nint threadHandle;
        private int processId;
        private int threadId;

        public nint ProcessHandle
        {
            readonly get => processHandle;
            set => processHandle = value;
        }

        public nint ThreadHandle
        {
            readonly get => threadHandle;
            set => threadHandle = value;
        }

        public int ProcessId
        {
            readonly get => processId;
            set => processId = value;
        }

        public int ThreadId
        {
            readonly get => threadId;
            set => threadId = value;
        }
    }
}
