using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;

using SleepHunter.Win32;

namespace SleepHunter.IO.Process
{
    internal sealed class ProcessMemoryScanner : IDisposable
    {
        private const int DefaultPageSize = 0x1000; 
        private const uint MinimumVmAddress = 0x0040_0000;
        private const uint MaximumVmAddress = 0xFFFF_FFFF;

        private bool isDisposed;
        private nint processHandle;
        private readonly bool leaveOpen;
        private readonly int pageSize;

        private readonly byte[] internalBuffer = new byte[8];
        private readonly byte[] searchBuffer;

        public nint ProcessHandle
        {
            get => processHandle;
            private set => processHandle = value;
        }

        public ProcessMemoryScanner(nint processHandle, bool leaveOpen = false)
        {
            this.processHandle = processHandle;
            this.leaveOpen = leaveOpen;

            NativeMethods.GetNativeSystemInfo(out var sysInfo);
            pageSize = (int)sysInfo.PageSize;

            if (pageSize <= 0)
                pageSize = DefaultPageSize;

            searchBuffer = new byte[pageSize];
        }

        ~ProcessMemoryScanner() => Dispose(false);

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

            processHandle = 0;
            isDisposed = true;
        }

        public IEnumerable<nint> FindAllUInt32(uint value, long startingAddress = 0, long endingAddress = 0)
        {
            internalBuffer[0] = (byte)(value);
            internalBuffer[1] = (byte)(value >> 8);
            internalBuffer[2] = (byte)(value >> 16);
            internalBuffer[3] = (byte)(value >> 24);

            return FindAll(internalBuffer, 4, startingAddress, endingAddress);
        }

        private IEnumerable<nint> FindAll(byte[] bytes, int size, long startingAddress, long endingAddress)
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
                var baseAddress = address;
                var queryResult = (int)NativeMethods.VirtualQueryEx(processHandle, (nint)baseAddress, out var memoryInfo, sizeofMemoryInfo);

                if (queryResult <= 0)
                    throw new Win32Exception();

                if (memoryInfo.Type == VirtualMemoryType.Private && memoryInfo.State == VirtualMemoryStatus.Commit && memoryInfo.Protect == VirtualMemoryProtection.ReadWrite)
                {
                    var numberOfPages = Math.Ceiling((float)memoryInfo.RegionSize / pageSize);

                    for (int i = 0; i < numberOfPages; i++)
                    {
                        var result = NativeMethods.ReadProcessMemory(processHandle, memoryInfo.BaseAddress + (i * pageSize), searchBuffer, searchBuffer.Length, out var numberOfBytesRead);

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
