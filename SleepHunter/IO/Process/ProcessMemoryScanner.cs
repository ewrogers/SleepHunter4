using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

using SleepHunter.Win32;

namespace SleepHunter.IO.Process
{
    internal sealed class ProcessMemoryScanner : IDisposable
    {
        private static readonly int PageSize = 64 * 1024;
        private static readonly uint MinimumVmAddress = 0x00400000;
        private static readonly uint MaximumVmAddress = 0xFFFFFFFF;

        private bool isDisposed;
        private readonly bool leaveOpen;        
        private readonly byte[] internalBuffer = new byte[8];
        private readonly byte[] internalStringBuffer = new byte[4096];
        private readonly byte[] searchBuffer;

        public IntPtr ProcessHandle { get; private set; }

        public ProcessMemoryScanner(IntPtr processHandle, bool leaveOpen = false)
        {
            ProcessHandle = processHandle;
            this.leaveOpen = leaveOpen;

            searchBuffer = new byte[PageSize];
        }

        ~ProcessMemoryScanner() => Dispose(false);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool isDisposing)
        {
            if (isDisposed)
                return;

            if (isDisposing) { }

            if (!leaveOpen)
                NativeMethods.CloseHandle(ProcessHandle);

            ProcessHandle = IntPtr.Zero;
            isDisposed = true;
        }

        private void CheckIfDisposed()
        {
            if (isDisposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        public IntPtr FindByte(byte value, long startingAddress = 0, long endingAddress = 0)
        {
            CheckIfDisposed();

            internalBuffer[0] = value;
            return Find(internalBuffer, 1, startingAddress, endingAddress);
        }

        public IntPtr FindInt16(short value, long startingAddress = 0, long endingAddress = 0)
        {
            CheckIfDisposed();
            return FindUInt16((ushort)value, startingAddress, endingAddress);
        }

        public IntPtr FindInt32(int value, long startingAddress = 0, long endingAddress = 0)
        {
            CheckIfDisposed();
            return FindUInt32((uint)value, startingAddress, endingAddress);
        }

        public IntPtr FindInt64(long value, long startingAddress = 0, long endingAddress = 0)
        {
            CheckIfDisposed();
            return FindUInt64((ulong)value, startingAddress, endingAddress);
        }

        public IntPtr FindUInt16(ushort value, long startingAddress = 0, long endingAddress = 0)
        {
            CheckIfDisposed();

            internalBuffer[0] = (byte)(value);
            internalBuffer[1] = (byte)(value >> 8);

            return Find(internalBuffer, 2, startingAddress, endingAddress);
        }

        public IntPtr FindUInt32(uint value, long startingAddress = 0, long endingAddress = 0)
        {
            CheckIfDisposed();

            internalBuffer[0] = (byte)(value);
            internalBuffer[1] = (byte)(value >> 8);
            internalBuffer[2] = (byte)(value >> 16);
            internalBuffer[3] = (byte)(value >> 24);

            return Find(internalBuffer, 4, startingAddress, endingAddress);
        }

        public IntPtr FindUInt64(ulong value, long startingAddress = 0, long endingAddress = 0)
        {
            CheckIfDisposed();

            internalBuffer[0] = (byte)(value);
            internalBuffer[1] = (byte)(value >> 8);
            internalBuffer[2] = (byte)(value >> 16);
            internalBuffer[3] = (byte)(value >> 24);
            internalBuffer[4] = (byte)(value >> 32);
            internalBuffer[5] = (byte)(value >> 40);
            internalBuffer[6] = (byte)(value >> 48);
            internalBuffer[7] = (byte)(value >> 56);

            return Find(internalBuffer, 8, startingAddress, endingAddress);
        }

        public IntPtr FindString(string value, long startingAddress = 0, long endingAddress = 0)
        {
            CheckIfDisposed();

            if (value.Length >= internalStringBuffer.Length)
                throw new InvalidOperationException("Length exceeded the buffer size");

            Encoding.ASCII.GetBytes(value, 0, value.Length, internalStringBuffer, 0);
            return Find(internalStringBuffer, value.Length, startingAddress, endingAddress);
        }

        public IntPtr Find(byte[] bytes, int size, long startingAddress = 0, long endingAddress = 0)
        {
            CheckIfDisposed();

            var start = (uint)startingAddress;
            var end = (uint)endingAddress;

            if (start <= 0)
                start = MinimumVmAddress;

            if (end <= 0)
                end = MaximumVmAddress;

            long address = start;
            int sizeofMemoryInfo = Marshal.SizeOf(typeof(MemoryBasicInformation));

            while (address <= end)
            {
                var queryResult = (int)NativeMethods.VirtualQueryEx(ProcessHandle, (IntPtr)address, out var memoryInfo, (IntPtr)sizeofMemoryInfo);

                if (queryResult <= 0)
                    break;

                if (memoryInfo.Type == VirtualMemoryType.Private && memoryInfo.State == VirtualMemoryStatus.Commit && memoryInfo.Protect == VirtualMemoryProtection.ReadWrite)
                {
                    var numberOfPages = memoryInfo.RegionSize / PageSize;

                    for (int i = 0; i < numberOfPages; i++)
                    {
                        var count = (IntPtr)searchBuffer.Length;
                        var result = NativeMethods.ReadProcessMemory(ProcessHandle, memoryInfo.BaseAddress + (i * PageSize), searchBuffer, count, out var numberOfBytesRead);

                        if (!result || numberOfBytesRead != searchBuffer.Length)
                            throw new Win32Exception("Unable to read memory page from process.");

                        var index = IndexOfSequence(searchBuffer, bytes, size);

                        if (index >= 0)
                            return memoryInfo.BaseAddress + (i * PageSize) + index;
                    }
                }

                address = (uint)memoryInfo.BaseAddress + memoryInfo.RegionSize;
            }

            return IntPtr.Zero;
        }

        private static int IndexOfSequence(byte[] sourceArray, byte[] patternArray, int patternSize)
        {
            for (int i = 0; i < sourceArray.Length; i++)
            {
                if (sourceArray.Length - i < patternSize)
                    return -1;

                if (patternArray[0] != sourceArray[i])
                    continue;

                for (int j = 0; j < patternSize; j++)
                {
                    if (sourceArray[i + j] != patternArray[j])
                        break;

                    if (j == patternSize - 1)
                        return i;
                }
            }

            return -1;
        }
    }
}
