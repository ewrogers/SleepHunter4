using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;

using SleepHunter.Win32;

namespace SleepHunter.IO.Process
{
    public sealed class ProcessMemoryAccessor : IDisposable
    {
        private bool isDisposed;
        private readonly int processId;
        private nint processHandle;
        private readonly ProcessAccess access;

        public int ProcessId => processId;
        public nint ProcessHandle => processHandle;
        public ProcessAccess Access => access;

        public ProcessMemoryAccessor(int processId, ProcessAccess access = ProcessAccess.ReadWrite)
        {
            this.processId = processId;
            this.access = access;

            processHandle = NativeMethods.OpenProcess(access.ToWin32Flags(), false, processId);

            if (processHandle == 0)
                throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Unable to open process {processId}: {Marshal.GetLastPInvokeErrorMessage()}");
        }

        public Stream GetStream()
        {
            CheckIfDisposed();
            return new ProcessMemoryStream(processHandle, ProcessAccess.Read, leaveOpen: true);
        }

        public Stream GetWriteableStream()
        {
            CheckIfDisposed();

            if (!access.HasFlag(ProcessAccess.Write))
                throw new InvalidOperationException("Accessor is not writeable");

            return new ProcessMemoryStream(processHandle, ProcessAccess.ReadWrite, leaveOpen: true);
        }

        ~ProcessMemoryAccessor() => Dispose(false);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool isDisposing)
        {
            if (isDisposed)
                return;

            if (isDisposing)
            {
                // Do any additional cleanup of managed resources here
            }

            if (processHandle != 0)
                NativeMethods.CloseHandle(processHandle);

            processHandle = 0;
            isDisposed = true;
        }

        private void CheckIfDisposed()
        {
            if (isDisposed)
                throw new ObjectDisposedException(GetType().Name);
        }
    }
}
