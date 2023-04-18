using System;
using System.ComponentModel;
using System.IO;

using SleepHunter.Win32;

namespace SleepHunter.IO.Process
{
    internal sealed class ProcessMemoryAccessor : IDisposable
    {
        private bool isDisposed;
        private int processId;
        private IntPtr processHandle;
        private ProcessAccess access;

        public int ProcessId
        {
            get { return processId; }
        }

        public IntPtr ProcessHandle
        {
            get { return processHandle; }
        }

        public ProcessAccess Access
        {
            get { return access; }
        }

        public ProcessMemoryAccessor(int processId, ProcessAccess access = ProcessAccess.ReadWrite)
        {
            this.processId = processId;
            processHandle = NativeMethods.OpenProcess(access.ToWin32Flags(), false, processId);

            if (processHandle == IntPtr.Zero)
            {
                var error = NativeMethods.GetLastError();
                throw new Win32Exception(error, "Unable to open process.");
            }

            this.access = access;
        }

        public Stream GetStream()
        {
            return new ProcessMemoryStream(processHandle, access, leaveOpen: true);
        }

        #region IDisposable Methods
        ~ProcessMemoryAccessor()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool isDisposing)
        {
            if (isDisposed)
                return;

            if (isDisposing)
            {
            }

            if (processHandle != IntPtr.Zero)
                NativeMethods.CloseHandle(processHandle);

            processHandle = IntPtr.Zero;
            isDisposed = true;
        }
        #endregion
    }
}
