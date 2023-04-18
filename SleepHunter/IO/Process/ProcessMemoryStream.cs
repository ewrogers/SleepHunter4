using System;
using System.ComponentModel;
using System.IO;

using SleepHunter.Win32;

namespace SleepHunter.IO.Process
{
    internal sealed class ProcessMemoryStream : Stream
    {
        private bool isDisposed;
        private readonly bool leaveOpen;
        private readonly byte[] internalBuffer = new byte[4096];

        public override bool CanRead => ProcessHandle != IntPtr.Zero && Access.HasFlag(ProcessAccess.Read);
        public override bool CanSeek => ProcessHandle != IntPtr.Zero;
        public override bool CanWrite => ProcessHandle != IntPtr.Zero && Access.HasFlag(ProcessAccess.Write);
        public override bool CanTimeout { get { return false; } }

        public IntPtr ProcessHandle { get; set; }

        public ProcessAccess Access { get; }

        public override long Length => throw new NotSupportedException("Length is not supported.");

        public override long Position { get; set; } = 0x400000;

        ~ProcessMemoryStream() => Dispose(false);

        public ProcessMemoryStream(IntPtr processHandle, ProcessAccess access = ProcessAccess.ReadWrite, bool leaveOpen = true)
        {
            ProcessHandle = processHandle;
            Access = access;

            this.leaveOpen = leaveOpen;
        }

        public override void Close()
        {
            if (ProcessHandle != IntPtr.Zero && !leaveOpen)
                NativeMethods.CloseHandle(ProcessHandle);

            ProcessHandle = IntPtr.Zero;
            base.Close();
        }

        public override void Flush() => CheckIfDisposed();

        public override int Read(byte[] buffer, int offset, int count)
        {
            CheckIfDisposed();
            CheckBufferSize(count);

            var success = NativeMethods.ReadProcessMemory(ProcessHandle, (IntPtr)Position, internalBuffer, (IntPtr)count, out var numberOfBytesRead);

            if (!success || numberOfBytesRead != count)
            {
                var error = NativeMethods.GetLastError();
                throw new Win32Exception(error, "Unable to read from process memory.");
            }

            Position += numberOfBytesRead;

            Buffer.BlockCopy(internalBuffer, 0, buffer, offset, count);
            return numberOfBytesRead;
        }

        public override int ReadByte()
        {
            CheckIfDisposed();

            var success = NativeMethods.ReadProcessMemory(ProcessHandle, (IntPtr)Position, internalBuffer, (IntPtr)1, out var numberOfBytesRead);

            if (!success || numberOfBytesRead != 1)
            {
                var error = NativeMethods.GetLastError();
                throw new Win32Exception(error, "Unable to read from process memory.");
            }

            Position += numberOfBytesRead;

            return internalBuffer[0];
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            CheckIfDisposed();

            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;

                case SeekOrigin.Current:
                    Position += offset;
                    break;

                case SeekOrigin.End:
                    Position -= offset;
                    break;
            }

            return Position;
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

            var success = NativeMethods.WriteProcessMemory(ProcessHandle, (IntPtr)Position, internalBuffer, (IntPtr)count, out var numberOfBytesWritten);

            if (!success || numberOfBytesWritten != count)
            {
                var error = NativeMethods.GetLastError();
                throw new Win32Exception(error, "Unable to write to process memory.");
            }


            Position += numberOfBytesWritten;
        }

        public override void WriteByte(byte value)
        {
            CheckIfDisposed();

            internalBuffer[0] = value;

            var success = NativeMethods.WriteProcessMemory(ProcessHandle, (IntPtr)Position, internalBuffer, (IntPtr)1, out var numberOfBytesWritten);

            if (!success || numberOfBytesWritten != 1)
            {
                var error = NativeMethods.GetLastError();
                throw new Win32Exception(error, "Unable to write to process memory.");
            }

            Position += numberOfBytesWritten;
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposed)
                return;

            if (isDisposing)
            {

            }

            if (ProcessHandle != IntPtr.Zero && !leaveOpen)
                NativeMethods.CloseHandle(ProcessHandle);

            ProcessHandle = IntPtr.Zero;

            base.Dispose(isDisposing);
            isDisposed = true;
        }

        private void CheckIfDisposed()
        {
            if (isDisposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        private void CheckBufferSize(int count)
        {
            if (internalBuffer.Length < count)
                throw new InvalidOperationException("Length exceeds buffer length.");
        }
    }
}
