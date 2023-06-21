using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;

using SleepHunter.Win32;

namespace SleepHunter.IO.Process
{
    internal sealed class ProcessMemoryStream : Stream
    {
        private bool isDisposed;
        private nint processHandle;
        private readonly ProcessAccess access;
        private long position = 0x40_0000;
        private byte[] internalBuffer = new byte[256];
        private readonly bool leaveOpen;

        public override bool CanRead => processHandle != 0 && access.HasFlag(ProcessAccess.Read);
        public override bool CanSeek => processHandle != 0;
        public override bool CanWrite => processHandle != 0 && access.HasFlag(ProcessAccess.Write);
        public override bool CanTimeout => false;

        public nint ProcessHandle
        {
            get => processHandle;
            private set => processHandle = value;
        }

        public override long Length => throw new NotSupportedException($"{nameof(Length)} is not supported.");

        public override long Position
        {
            get => position;
            set => position = value;
        }

        ~ProcessMemoryStream() => Dispose(false);

        public ProcessMemoryStream(nint processHandle, ProcessAccess access = ProcessAccess.ReadWrite, bool leaveOpen = true)
        {
            this.access = access;
            this.processHandle = processHandle;
            this.leaveOpen = leaveOpen;
        }

        public override void Close()
        {
            if (processHandle != 0 && !leaveOpen)
                NativeMethods.CloseHandle(processHandle);

            processHandle = 0;
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

            var readPosition = position;
            bool success = NativeMethods.ReadProcessMemory(processHandle, (nint)position, internalBuffer, count, out var numberOfBytesRead);

            if (!success || numberOfBytesRead != count)
                throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Unable to read process memory at 0x{readPosition:X}: {Marshal.GetLastPInvokeErrorMessage()}");

            position += numberOfBytesRead;

            Buffer.BlockCopy(internalBuffer, 0, buffer, offset, count);
            return (int)numberOfBytesRead;
        }

        public override int ReadByte()
        {
            CheckIfDisposed();

            var readPosition = position;
            bool success = NativeMethods.ReadProcessMemory(processHandle, (nint)position, internalBuffer, 1, out var numberOfBytesRead);

            if (!success || numberOfBytesRead != 1)
                throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Unable to read process memory at 0x{readPosition:X}: {Marshal.GetLastPInvokeErrorMessage()}");

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

            throw new NotSupportedException($"{nameof(SetLength)} is not supported.");
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            CheckIfDisposed();
            CheckBufferSize(count);

            Buffer.BlockCopy(buffer, offset, internalBuffer, 0, count);

            var writePosition = position;
            bool success = NativeMethods.WriteProcessMemory(processHandle, (nint)position, internalBuffer, count, out var numberOfBytesWritten);

            if (!success || numberOfBytesWritten != count)
                throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Unable to write process memory at 0x{writePosition:X}: {Marshal.GetLastPInvokeErrorMessage()}");

            position += numberOfBytesWritten;
        }

        public override void WriteByte(byte value)
        {
            CheckIfDisposed();

            internalBuffer[0] = value;

            var writePosition = position;
            bool success = NativeMethods.WriteProcessMemory(processHandle, (nint)position, internalBuffer, 1, out var numberOfBytesWritten);

            if (!success || numberOfBytesWritten != 1)
                throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Unable to write process memory at 0x{writePosition:X}: {Marshal.GetLastPInvokeErrorMessage()}");

            position += numberOfBytesWritten;
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposed)
                return;

            if (isDisposing)
            {

            }

            if (processHandle != 0 && !leaveOpen)
                NativeMethods.CloseHandle(processHandle);

            processHandle = 0;

            base.Dispose(isDisposing);
            isDisposed = true;
        }

        private void CheckIfDisposed()
        {
            if (isDisposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        private void CheckBufferSize(int count, bool copyContents = false)
        {
            if (internalBuffer.Length >= count)
                return;

            var newBuffer = new byte[count];

            if (copyContents)
                Buffer.BlockCopy(internalBuffer, 0, newBuffer, 0, internalBuffer.Length);

            internalBuffer = newBuffer;
        }
    }
}
