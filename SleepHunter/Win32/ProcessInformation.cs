using System;
using System.Runtime.InteropServices;

namespace SleepHunter.Win32
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct ProcessInformation
    {
        nint processHandle;
        nint threadHandle;
        int processId;
        int threadId;

        public nint ProcessHandle
        {
            get { return processHandle; }
            set { processHandle = value; }
        }

        public nint ThreadHandle
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
