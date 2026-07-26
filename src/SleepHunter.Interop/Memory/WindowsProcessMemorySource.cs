using System.Buffers;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace SleepHunter.Interop.Memory;

public sealed class WindowsProcessMemorySource : IProcessMemorySource
{
    private const int InvalidHandleError = 6;
    private const int InvalidAddressError = 487;

    private readonly SafeProcessHandle processHandle;

    public WindowsProcessMemorySource(SafeProcessHandle processHandle)
    {
        ArgumentNullException.ThrowIfNull(processHandle);

        if (processHandle.IsInvalid || processHandle.IsClosed)
        {
            throw new ArgumentException(
                "The process handle must be open and valid.",
                nameof(processHandle));
        }

        this.processHandle = processHandle;
    }

    public MemorySourceReadResult Read(
        MemoryAddress address,
        Span<byte> destination)
    {
        if (destination.IsEmpty)
        {
            return MemorySourceReadResult.Failed();
        }

        if (processHandle.IsInvalid || processHandle.IsClosed)
        {
            return MemorySourceReadResult.Failed(InvalidHandleError);
        }

        if (address.Value > (ulong)nuint.MaxValue)
        {
            return MemorySourceReadResult.Failed(InvalidAddressError);
        }

        var buffer = ArrayPool<byte>.Shared.Rent(destination.Length);
        try
        {
            var success = ReadProcessMemory(
                processHandle,
                (nuint)address.Value,
                buffer,
                (nuint)destination.Length,
                out var bytesRead);
            if (bytesRead > (nuint)destination.Length)
            {
                throw new InvalidOperationException(
                    "ReadProcessMemory returned more bytes than requested.");
            }

            var count = checked((int)bytesRead);
            buffer.AsSpan(0, count).CopyTo(destination);
            return new MemorySourceReadResult(
                count,
                success ? 0 : Marshal.GetLastWin32Error());
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
        }
    }

    [DllImport(
        "kernel32.dll",
        EntryPoint = "ReadProcessMemory",
        SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ReadProcessMemory(
        SafeProcessHandle processHandle,
        nuint baseAddress,
        [Out] byte[] buffer,
        nuint size,
        out nuint bytesRead);
}
