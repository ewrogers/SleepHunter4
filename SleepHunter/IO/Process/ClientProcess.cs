using System;
using System.Text;

using SleepHunter.Common;
using SleepHunter.Win32;

namespace SleepHunter.IO.Process
{
    public sealed class ClientProcess : ObservableObject
    {
        private const int ViewportWidth = 640;
        private const int ViewportHeight = 480;

        private int processId;
        private nint windowHandle;
        private string windowClassName = string.Empty;
        private string windowTitle = string.Empty;
        private int windowWidth = ViewportWidth;
        private int windowHeight = ViewportHeight;
        private DateTime creationTime;

        public event EventHandler ProcessUpdated;

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

        public string WindowClassName
        {
            get => windowClassName;
            set => SetProperty(ref windowClassName, value);
        }

        public string WindowTitle
        {
            get => windowTitle;
            set => SetProperty(ref windowTitle, value);
        }

        public int WindowWidth
        {
            get => windowWidth;
            set => SetProperty(ref windowWidth, value, onChanged: (p) => { RaisePropertyChanged(nameof(WindowScaleX)); });
        }

        public int WindowHeight
        {
            get => windowHeight;
            set => SetProperty(ref windowHeight, value, onChanged: (p) => { RaisePropertyChanged(nameof(WindowScaleY)); });
        }

        public double WindowScaleX => WindowWidth / 640.0;

        public double WindowScaleY => WindowHeight / 480.0;

        public DateTime CreationTime
        {
            get => creationTime;
            set => SetProperty(ref creationTime, value);
        }

        public ClientProcess() { }

        public void Update()
        {
            var windowTextLength = NativeMethods.GetWindowTextLength(windowHandle);
            var windowTextBuffer = new StringBuilder(windowTextLength + 1);
            windowTextLength = NativeMethods.GetWindowText(windowHandle, windowTextBuffer, windowTextBuffer.Capacity);

            WindowTitle = windowTextBuffer.ToString(0, windowTextLength);

            if (NativeMethods.GetClientRect(windowHandle, out var clientRect))
            {
                WindowWidth = clientRect.Width;
                WindowHeight = clientRect.Height;
            }

            ProcessUpdated?.Invoke(this, EventArgs.Empty);
        }
    }
}
