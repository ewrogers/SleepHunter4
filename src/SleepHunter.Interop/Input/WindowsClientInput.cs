using System.Runtime.InteropServices;

namespace SleepHunter.Interop.Input;

public sealed class WindowsClientWindowGuard : IClientWindowGuard
{
    public ClientWindowValidationResult Validate(ClientWindowTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);

        if (!NativeMethods.IsWindow(target.WindowHandle))
        {
            return new ClientWindowValidationResult(
                ClientWindowValidationFailure.WindowUnavailable,
                "The target client window is no longer available.");
        }

        _ = NativeMethods.GetWindowThreadProcessId(
            target.WindowHandle,
            out var processId);
        if (processId == 0)
        {
            return new ClientWindowValidationResult(
                ClientWindowValidationFailure.WindowUnavailable,
                "The target client window process could not be resolved.",
                Marshal.GetLastPInvokeError());
        }

        if (processId != target.ProcessId)
        {
            return new ClientWindowValidationResult(
                ClientWindowValidationFailure.ProcessMismatch,
                "The target window is now owned by a different process.");
        }

        if (!NativeMethods.GetClientRect(
                target.WindowHandle,
                out var clientRectangle))
        {
            return new ClientWindowValidationResult(
                ClientWindowValidationFailure.ClientAreaUnavailable,
                "The target client area could not be measured.",
                Marshal.GetLastPInvokeError());
        }

        if (clientRectangle.Width != target.ClientWidth ||
            clientRectangle.Height != target.ClientHeight)
        {
            return new ClientWindowValidationResult(
                ClientWindowValidationFailure.ClientAreaChanged,
                "The target client area changed after input was planned.");
        }

        return ClientWindowValidationResult.Valid;
    }
}

public sealed class WindowsWindowMessageSink : IWindowMessageSink
{
    public bool TryPost(
        ClientWindowTarget target,
        WindowInputMessage message,
        out int nativeErrorCode)
    {
        ArgumentNullException.ThrowIfNull(target);

        var posted = NativeMethods.PostMessage(
            target.WindowHandle,
            (uint)message.Message,
            message.WParam,
            message.LParam);
        nativeErrorCode = posted
            ? 0
            : Marshal.GetLastPInvokeError();
        return posted;
    }
}

internal static partial class NativeMethods
{
    [LibraryImport("user32.dll", EntryPoint = "IsWindow")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool IsWindow(nint windowHandle);

    [LibraryImport(
        "user32.dll",
        EntryPoint = "GetWindowThreadProcessId",
        SetLastError = true)]
    internal static partial uint GetWindowThreadProcessId(
        nint windowHandle,
        out int processId);

    [LibraryImport(
        "user32.dll",
        EntryPoint = "GetClientRect",
        SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetClientRect(
        nint windowHandle,
        out NativeRectangle rectangle);

    [LibraryImport(
        "user32.dll",
        EntryPoint = "PostMessageW",
        SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool PostMessage(
        nint windowHandle,
        uint message,
        nuint wParam,
        nint lParam);

    [LibraryImport("user32.dll", EntryPoint = "MapVirtualKeyW")]
    internal static partial uint MapVirtualKey(
        uint code,
        uint mapType);
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct NativeRectangle
{
    public int Left { get; init; }

    public int Top { get; init; }

    public int Right { get; init; }

    public int Bottom { get; init; }

    public int Width => Right - Left;

    public int Height => Bottom - Top;
}
