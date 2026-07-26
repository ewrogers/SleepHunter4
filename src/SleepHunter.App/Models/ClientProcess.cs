using System;
using System.Text;
using SleepHunter.Common;
using SleepHunter.Win32;

namespace SleepHunter.Models
{
    public sealed class ClientProcess : ObservableObject
    {
        private int processId;
        private nint windowHandle;
        private string windowTitle = string.Empty;
        private DateTime creationTime;

        public int ProcessId
        {
            get => processId;
            set => SetProperty(ref processId, value);
        }

        public nint WindowHandle
        {
            get => windowHandle;
            set => SetProperty(ref windowHandle, value);
        }

        public string WindowTitle
        {
            get => windowTitle;
            set => SetProperty(ref windowTitle, value);
        }

        public DateTime CreationTime
        {
            get => creationTime;
            set => SetProperty(ref creationTime, value);
        }

        public void Refresh()
        {
            var windowTextLength =
                NativeMethods.GetWindowTextLength(windowHandle);
            var windowTextBuffer =
                new StringBuilder(windowTextLength + 1);
            windowTextLength = NativeMethods.GetWindowText(
                windowHandle,
                windowTextBuffer,
                windowTextBuffer.Capacity);
            WindowTitle = windowTextBuffer.ToString(
                0,
                windowTextLength);

            if (CreationTime != DateTime.MinValue)
                return;

            using var process =
                System.Diagnostics.Process.GetProcessById(
                    processId);
            CreationTime = process.StartTime;
        }
    }
}
