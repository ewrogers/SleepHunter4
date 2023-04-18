using System;
using System.ComponentModel;
using System.IO;

using SleepHunter.Win32;

namespace SleepHunter.IO.Process
{
    internal sealed class ProcessMemoryStream : Stream
    {
        private bool isDisposed;
        private IntPtr processHandle;
        private ProcessAccess access;
        private long position = 0x400000;
        private readonly byte[] internalBuffer = new byte[4096];
        private readonly bool leaveOpen;

        public override bool CanRead { get { return processHandle != IntPtr.Zero && access.HasFlag(ProcessAccess.Read); } }
        public override bool CanSeek { get { return processHandle != IntPtr.Zero; } }
        public override bool CanWrite { get { return processHandle != IntPtr.Zero && access.HasFlag(ProcessAccess.Write); } }
        public override bool CanTimeout { get { return false; } }

        public IntPtr ProcessHandle
        {
            get { return processHandle; }
            private set { processHandle = value; }
        }

        public override long Length
        {
            get { throw new NotSupportedException("Length is not supported."); }
        }

        public override long Position
        {
            get { return position; }
            set { position = value; }
        }

        ~ProcessMemoryStream()
        {
            Dispose(false);
        }

        public ProcessMemoryStream(IntPtr processHandle, ProcessAccess access = ProcessAccess.ReadWrite, bool leaveOpen = true)
        {
            this.access = access;
            this.processHandle = processHandle;
            this.leaveOpen = leaveOpen;
        }

        public override void Close()
        {
            if (processHandle != IntPtr.Zero && !leaveOpen)
                NativeMethods.CloseHandle(processHandle);

            processHandle = IntPtr.Zero;

            base.Close();
        }

        public override void Flush()
        {
            CheckIfDisposed();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            CheckIfDisposed();
            CheckBufferSize(count);

            var success = NativeMethods.ReadProcessMemory(processHandle, (IntPtr)position, internalBuffer, (IntPtr)count, out var numberOfBytesRead);

            if (!success || numberOfBytesRead != count)
            {
                var error = NativeMethods.GetLastError();
                throw new Win32Exception(error, "Unable to read from process memory.");
            }

            position += numberOfBytesRead;

            Buffer.BlockCopy(internalBuffer, 0, buffer, offset, count);
            return numberOfBytesRead;
        }

        public override int ReadByte()
        {
            CheckIfDisposed();

            var success = NativeMethods.ReadProcessMemory(processHandle, (IntPtr)position, internalBuffer, (IntPtr)1, out var numberOfBytesRead);

            if (!success || numberOfBytesRead != 1)
            {
                var error = NativeMethods.GetLastError();
                throw new Win32Exception(error, "Unable to read from process memory.");
            }

            position += numberOfBytesRead;

            return internalBuffer[0];
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            CheckIfDisposed();

            switch (origin)
            {
                case SeekOrigin.Begin:
                    position = offset;
                    break;

                case SeekOrigin.Current:
                    position += offset;
                    break;

                case SeekOrigin.End:
                    position -= offset;
                    break;
            }

            return position;
        }

        public override void SetLength(long value)
        {
            CheckIfDisposed();

            throw new NotSupportedException("SetLength is not supported.");
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            CheckIfDisposed();
            CheckBufferSize(count);

            Buffer.BlockCopy(buffer, offset, internalBuffer, 0, count);

            var success = NativeMethods.WriteProcessMemory(processHandle, (IntPtr)position, internalBuffer, (IntPtr)count, out var numberOfBytesWritten);

            if (!success || numberOfBytesWritten != count)
            {
                var error = NativeMethods.GetLastError();
                throw new Win32Exception(error, "Unable to write to process memory.");
            }


            position += numberOfBytesWritten;
        }

        public override void WriteByte(byte value)
        {
            CheckIfDisposed();

            internalBuffer[0] = value;

            var success = NativeMethods.WriteProcessMemory(processHandle, (IntPtr)position, internalBuffer, (IntPtr)1, out var numberOfBytesWritten);

            if (!success || numberOfBytesWritten != 1)
            {
                var error = NativeMethods.GetLastError();
                throw new Win32Exception(error, "Unable to write to process memory.");
            }

            position += numberOfBytesWritten;
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposed)
                return;

            if (isDisposing)
            {

            }

            if (processHandle != IntPtr.Zero && !leaveOpen)
                NativeMethods.CloseHandle(processHandle);

            processHandle = IntPtr.Zero;

            base.Dispose(isDisposing);
            isDisposed = true;
        }

        void CheckIfDisposed()
        {
            if (isDisposed)
                throw new ObjectDisposedException("ProcessMemoryStream");
        }

        void CheckBufferSize(int count)
        {
            if (internalBuffer.Length < count)
                throw new InvalidOperationException("Length exceeds buffer length");
        }
    }
}
