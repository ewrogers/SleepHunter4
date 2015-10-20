using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Text;

namespace SleepHunter.Win32
{
   [return:MarshalAs(UnmanagedType.Bool)]
   internal delegate bool EnumWindowsProc(IntPtr windowHandle, IntPtr lParam);

   internal static class NativeMethods
   {
      [DllImport("user32", EntryPoint = "EnumWindows")]
      [return: MarshalAs(UnmanagedType.Bool)]
      internal static extern bool EnumWindows(EnumWindowsProc enumWindowProc, IntPtr lParam);

      [DllImport("user32", EntryPoint = "GetClassName")]
      internal static extern int GetClassName(IntPtr windowHandle, StringBuilder className, int maxLength);

      [DllImport("user32", EntryPoint = "GetWindowTextLength")]
      internal static extern int GetWindowTextLength(IntPtr windowHandle);

      [DllImport("user32", EntryPoint = "GetWindowText")]
      internal static extern int GetWindowText(IntPtr windowHandle, StringBuilder windowText, int maxLength);

      [DllImport("user32", EntryPoint = "GetWindowThreadProcessId")]
      internal static extern int GetWindowThreadProcessId(IntPtr windowHandle, out int processId);

      [DllImport("user32", EntryPoint = "GetWindowRect")]
      [return: MarshalAs(UnmanagedType.Bool)]
      internal static extern bool GetWindowRect(IntPtr windowHandle, out Rect windowRectangle);

      [DllImport("user32", EntryPoint = "PostMessage")]
      [return: MarshalAs(UnmanagedType.Bool)]
      internal static extern bool PostMessage(IntPtr windowHandle, uint message, uint wParam, uint lParam);

      [DllImport("user32", EntryPoint = "SendMessage")]
      [return: MarshalAs(UnmanagedType.Bool)]
      internal static extern bool SendMessage(IntPtr windowHandle, uint message, uint wParam, uint lParam);

      [DllImport("user32", EntryPoint = "VkKeyScan")]
      internal static extern ushort VkKeyScan(char character);

      [DllImport("user32", EntryPoint = "MapVirtualKey")]
      internal static extern uint MapVirtualKey(uint keyCode, VirtualKeyMapMode mapMode);

      [DllImport("user32", EntryPoint = "SetForegroundWindow")]
      [return: MarshalAs(UnmanagedType.Bool)]
      internal static extern bool SetForegroundWindow(IntPtr windowHandle);

      [DllImport("user32", EntryPoint = "RegisterHotKey")]
      [return: MarshalAs(UnmanagedType.Bool)]
      internal static extern bool RegisterHotKey(IntPtr windowHandle, int hotkeyId, ModifierKeys modifiers, int virtualKey);

      [DllImport("user32", EntryPoint = "UnregisterHotKey")]
      [return: MarshalAs(UnmanagedType.Bool)]
      internal static extern bool UnregisterHotKey(IntPtr windowHandle, int hotkeyId);

      [DllImport("kernel32", EntryPoint = "CreateProcess")]
      [return: MarshalAs(UnmanagedType.Bool)]
      internal static extern bool CreateProcess(string applicationPath,
         string commandLineArgs,
         ref SecurityAttributes processSecurityAttributes,
         ref SecurityAttributes threadSecurityAttributes,
         bool inheritHandle,
         ProcessCreationFlags creationFlags,
         IntPtr environment,
         string currentDirectory,
         ref StartupInfo startupInfo,
         out ProcessInformation processInformation);

      [DllImport("kernel32", EntryPoint = "GlobalAddAtom")]
      internal static extern ushort GlobalAddAtom(string atomName);

      [DllImport("kernel32", EntryPoint = "GlobalDeleteAtom")]
      internal static extern ushort GlobalDeleteAtom(ushort atom);

      [DllImport("kernel32", EntryPoint = "OpenProcess")]
      internal static extern IntPtr OpenProcess(ProcessAccessFlags desiredAccess, bool inheritHandle, int processId);

      [DllImport("kernel32", EntryPoint = "ReadProcessMemory")]
      [return: MarshalAs(UnmanagedType.Bool)]
      internal static extern bool ReadProcessMemory(IntPtr processHandle, IntPtr baseAddress, byte[] buffer, int count, out int numberOfBytesRead);

      [DllImport("kernel32", EntryPoint = "WriteProcessMemory")]
      [return: MarshalAs(UnmanagedType.Bool)]
      internal static extern bool WriteProcessMemory(IntPtr processHandle, IntPtr baseAddress, byte[] buffer, int count, out int numberOfBytesWritten);

      [DllImport("kernel32", EntryPoint = "VirtualQueryEx")]
      internal static extern int VirtualQueryEx(IntPtr processHandle, IntPtr baseAddress, out MemoryBasicInformation memoryInformation, int size);

      [DllImport("kernel32", EntryPoint = "ResumeThread")]
      internal static extern int ResumeThread(IntPtr threadHandle);

      [DllImport("kernel32", EntryPoint = "CloseHandle")]
      [return: MarshalAs(UnmanagedType.Bool)]
      internal static extern bool CloseHandle(IntPtr handle);

      [DllImport("kernel32", EntryPoint = "GetSystemInfo")]
      internal static extern void GetSystemInfo(out SystemInfo systemInfo);

      [DllImport("kernel32", EntryPoint = "GetNativeSystemInfo")]
      internal static extern void GetNativeSystemInfo(out SystemInfo systemInfo);

      [DllImport("kernel32", EntryPoint = "GetLastError")]
      internal static extern int GetLastError();
   }
}
