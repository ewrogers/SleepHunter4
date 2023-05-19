using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using SleepHunter.Win32;

namespace SleepHunter.IO.Process
{
    internal sealed class ProcessMemoryScanner : IDisposable
    {
        static readonly uint MinimumVmAddress = 0x0040_0000;
        static readonly uint MaximumVmAddress = 0xFFFF_FFFF;

        bool isDisposed;
        IntPtr processHandle;
        bool leaveOpen;
        int pageSize;
        byte[] internalBuffer = new byte[8];
        byte[] internalStringBuffer = new byte[256];
        byte[] searchBuffer;

        public IntPtr ProcessHandle
        {
            get { return processHandle; }
            private set { processHandle = value; }
        }

        public ProcessMemoryScanner(IntPtr processHandle, bool leaveOpen = false)
        {
            this.processHandle = processHandle;
            this.leaveOpen = leaveOpen;

            NativeMethods.GetNativeSystemInfo(out var sysInfo);
            pageSize = (int)sysInfo.PageSize;

            searchBuffer = new byte[pageSize];
        }

        #region IDisposable Methods
        ~ProcessMemoryScanner()
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

            if (!leaveOpen)
                NativeMethods.CloseHandle(processHandle);

            processHandle = IntPtr.Zero;
            isDisposed = true;
        }
        #endregion

        public IntPtr FindByte(byte value, long startingAddress = 0, long endingAddress = 0)
        {
            internalBuffer[0] = value;
            return Find(internalBuffer, 1, startingAddress, endingAddress);
        }

        public IntPtr FindInt16(short value, long startingAddress = 0, long endingAddress = 0)
        {
            return FindUInt16((ushort)value, startingAddress, endingAddress);
        }

        public IntPtr FindInt32(int value, long startingAddress = 0, long endingAddress = 0)
        {
            return FindUInt32((uint)value, startingAddress, endingAddress);
        }

        public IntPtr FindInt64(long value, long startingAddress = 0, long endingAddress = 0)
        {
            return FindUInt64((ulong)value, startingAddress, endingAddress);
        }

        public IntPtr FindUInt16(ushort value, long startingAddress = 0, long endingAddress = 0)
        {
            internalBuffer[0] = (byte)(value);
            internalBuffer[1] = (byte)(value >> 8);

            return Find(internalBuffer, 2, startingAddress, endingAddress);
        }

        public IntPtr FindUInt32(uint value, long startingAddress = 0, long endingAddress = 0)
        {
            internalBuffer[0] = (byte)(value);
            internalBuffer[1] = (byte)(value >> 8);
            internalBuffer[2] = (byte)(value >> 16);
            internalBuffer[3] = (byte)(value >> 24);

            return Find(internalBuffer, 4, startingAddress, endingAddress);
        }

        public IntPtr FindUInt64(ulong value, long startingAddress = 0, long endingAddress = 0)
        {
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

        public IEnumerable<IntPtr> FindAllUInt32(uint value, long startingAddress = 0, long endingAddress = 0)
        {
            internalBuffer[0] = (byte)(value);
            internalBuffer[1] = (byte)(value >> 8);
            internalBuffer[2] = (byte)(value >> 16);
            internalBuffer[3] = (byte)(value >> 24);

            return FindAll(internalBuffer, 4, startingAddress, endingAddress);
        }

        public IntPtr FindString(string value, long startingAddress = 0, long endingAddress = 0)
        {
            if (value.Length >= internalStringBuffer.Length)
                internalStringBuffer = new byte[value.Length];

            Encoding.ASCII.GetBytes(value, 0, value.Length, internalStringBuffer, 0);
            return Find(internalStringBuffer, value.Length, startingAddress, endingAddress);
        }

        public IntPtr Find(byte[] bytes, int size, long startingAddress = 0, long endingAddress = 0) => FindAll(bytes, size, startingAddress, endingAddress).FirstOrDefault();

        public IEnumerable<IntPtr> FindAll(byte[] bytes, int size, long startingAddress = 0, long endingAddress = 0)
        {
            var start = startingAddress;
            var end = endingAddress;

            if (start <= 0)
                start = MinimumVmAddress;

            if (end <= 0)
                end = MaximumVmAddress;

            long address = start;
            int sizeofMemoryInfo = Marshal.SizeOf(typeof(MemoryBasicInformation));

            while (address <= end)
            {
                var baseAddress = (IntPtr)address;
                var queryResult = (int)NativeMethods.VirtualQueryEx(processHandle, baseAddress, out var memoryInfo, sizeofMemoryInfo);

                if (queryResult <= 0)
                {
                    var lastError = NativeMethods.GetLastError();
                    throw new Win32Exception(lastError);
                }

                if (memoryInfo.Type == VirtualMemoryType.Private && memoryInfo.State == VirtualMemoryStatus.Commit && memoryInfo.Protect == VirtualMemoryProtection.ReadWrite)
                {
                    var numberOfPages = Math.Ceiling((float)memoryInfo.RegionSize / pageSize);

                    for (int i = 0; i < numberOfPages; i++)
                    {
                        int numberOfBytesRead;
                        var result = NativeMethods.ReadProcessMemory(processHandle, memoryInfo.BaseAddress + (i * pageSize), searchBuffer, searchBuffer.Length, out numberOfBytesRead);

                        if (!result || numberOfBytesRead != searchBuffer.Length)
                            throw new Win32Exception("Unable to read memory page from process.");

                        var index = IndexOfSequence(searchBuffer, bytes, size);

                        if (index >= 0)
                            yield return memoryInfo.BaseAddress + (i * pageSize) + index;
                    }
                }

                address = (uint)memoryInfo.BaseAddress + memoryInfo.RegionSize;
            }
        }

        static int IndexOfSequence(byte[] sourceArray, byte[] patternArray, int patternSize)
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
