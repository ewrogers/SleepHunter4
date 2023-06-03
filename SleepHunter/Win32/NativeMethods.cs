using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Text;

using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace SleepHunter.Win32
{
    [return: MarshalAs(UnmanagedType.Bool)]
    internal delegate bool EnumWindowsProc(nint windowHandle, nint lParam);

    internal static class NativeMethods
    {
        [DllImport("user32", EntryPoint = "EnumWindows", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EnumWindows(EnumWindowsProc enumWindowProc, nint lParam);

        [DllImport("user32", EntryPoint = "GetClassName", CharSet = CharSet.Unicode)]
        internal static extern int GetClassName(nint windowHandle, StringBuilder className, int maxLength);

        [DllImport("user32", EntryPoint = "GetWindowTextLength", CharSet = CharSet.Auto)]
        internal static extern int GetWindowTextLength(nint windowHandle);

        [DllImport("user32", EntryPoint = "GetWindowText", CharSet = CharSet.Unicode)]
        internal static extern int GetWindowText(nint windowHandle, StringBuilder windowText, int maxLength);

        [DllImport("user32", EntryPoint = "SetWindowText", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetWindowText(nint windowHandle, string windowText);

        [DllImport("user32", EntryPoint = "GetWindowThreadProcessId", CharSet = CharSet.Auto)]
        internal static extern int GetWindowThreadProcessId(nint windowHandle, out int processId);

        [DllImport("user32", EntryPoint = "GetClientRect", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetClientRect(nint windowHandle, out Rect clientRectangle);

        [DllImport("user32", EntryPoint = "PostMessage", CharSet = CharSet.Auto)]
        internal static extern bool PostMessage(nint windowHandle, uint message, nuint wParam, nuint lParam);

        [DllImport("user32", EntryPoint = "SendMessage", CharSet = CharSet.Auto)]
        internal static extern nint SendMessage(nint windowHandle, uint message, nuint wParam, nuint lParam);

        [DllImport("user32", EntryPoint = "VkKeyScan", CharSet = CharSet.Auto)]
        internal static extern ushort VkKeyScan(char character);

        [DllImport("user32", EntryPoint = "MapVirtualKey", CharSet = CharSet.Auto)]
        internal static extern uint MapVirtualKey(uint keyCode, VirtualKeyMapMode mapMode);

        [DllImport("user32", EntryPoint = "SetForegroundWindow", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetForegroundWindow(nint windowHandle);

        [DllImport("user32", EntryPoint = "RegisterHotKey", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool RegisterHotKey(nint windowHandle, int hotkeyId, ModifierKeys modifiers, int virtualKey);

        [DllImport("user32", EntryPoint = "UnregisterHotKey", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool UnregisterHotKey(nint windowHandle, int hotkeyId);

        [DllImport("kernel32", EntryPoint = "CreateProcess", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CreateProcess(string applicationPath,
           string commandLineArgs,
           ref SecurityAttributes processSecurityAttributes,
           ref SecurityAttributes threadSecurityAttributes,
           bool inheritHandle,
           ProcessCreationFlags creationFlags,
           nint environment,
           string currentDirectory,
           ref StartupInfo startupInfo,
           out ProcessInformation processInformation);

        [DllImport("kernel32", EntryPoint = "GlobalAddAtom", CharSet = CharSet.Unicode)]
        internal static extern ushort GlobalAddAtom(string atomName);

        [DllImport("kernel32", EntryPoint = "GlobalDeleteAtom", CharSet = CharSet.Auto)]
        internal static extern ushort GlobalDeleteAtom(ushort atom);

        [DllImport("kernel32", EntryPoint = "GetProcessTimes", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetProcessTimes(nint processHandle, out FILETIME creationTime, out FILETIME exitTime, out FILETIME kernelTIme, out FILETIME userTime);

        [DllImport("kernel32", EntryPoint = "OpenProcess", CharSet = CharSet.Auto)]
        internal static extern nint OpenProcess(ProcessAccessFlags desiredAccess, bool inheritHandle, int processId);

        [DllImport("kernel32", EntryPoint = "ReadProcessMemory", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ReadProcessMemory(nint processHandle, nint baseAddress, byte[] buffer, nint count, out int numberOfBytesRead);

        [DllImport("kernel32", EntryPoint = "WriteProcessMemory", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool WriteProcessMemory(nint processHandle, nint baseAddress, byte[] buffer, nint count, out int numberOfBytesWritten);

        [DllImport("kernel32", EntryPoint = "VirtualQueryEx", CharSet = CharSet.Auto)]
        internal static extern nint VirtualQueryEx(nint processHandle, nint baseAddress, out MemoryBasicInformation memoryInformation, nint size);

        [DllImport("kernel32", EntryPoint = "ResumeThread", CharSet = CharSet.Auto)]
        internal static extern int ResumeThread(nint threadHandle);

        [DllImport("kernel32", EntryPoint = "CloseHandle", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CloseHandle(nint handle);

        [DllImport("kernel32", EntryPoint = "GetLastError", CharSet = CharSet.Auto)]
        internal static extern int GetLastError();

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetPhysicallyInstalledSystemMemory(out long totalMemoryKilobytes);

        [DllImport("kernel32.dll")]
        internal static extern void GetNativeSystemInfo(out SystemInfo systemInfo);
    }
}
