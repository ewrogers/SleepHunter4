using System;
using System.Runtime.InteropServices;

namespace SleepHunter.Win32
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct ProcessInformation
    {
        private IntPtr processHandle;
        private IntPtr threadHandle;
        private int processId;
        private int threadId;

        public IntPtr ProcessHandle
        {
            get { return processHandle; }
            set { processHandle = value; }
        }

        public IntPtr ThreadHandle
        {
            get { return threadHandle; }
            set { threadHandle = value; }
        }

        public int ProcessId
        {
            get { return processId; }
            set { processId = value; }
        }

        public int ThreadId
        {
            get { return threadId; }
            set { threadId = value; }
        }
    }
}
