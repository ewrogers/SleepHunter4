using System;
using System.Text;

using SleepHunter.Common;
using SleepHunter.Win32;

namespace SleepHunter.IO.Process
{
    internal sealed class ClientProcess : ObservableObject
    {
        private int processId;
        private IntPtr windowHandle;
        private string windowClassName = string.Empty;
        private string windowTitle = string.Empty;
        private int windowWidth = 640;
        private int windowHeight = 480;
        private DateTime creationTime;

        public int ProcessId
        {
            get { return processId; }
            set { SetProperty(ref processId, value); }
        }

        public IntPtr WindowHandle
        {
            get { return windowHandle; }
            set { SetProperty(ref windowHandle, value); }
        }

        public string WindowClassName
        {
            get { return windowClassName; }
            set { SetProperty(ref windowClassName, value); }
        }

        public string WindowTitle
        {
            get { return windowTitle; }
            set { SetProperty(ref windowTitle, value); }
        }

        public int WindowWidth
        {
            get { return windowWidth; }
            set { SetProperty(ref windowWidth, value, onChanged: (p) => { RaisePropertyChanged("WindowScaleX"); }); }
        }

        public int WindowHeight
        {
            get { return windowHeight; }
            set { SetProperty(ref windowHeight, value, onChanged: (p) => { RaisePropertyChanged("WindowScaleY"); }); }
        }

        public double WindowScaleX
        {
            get { return Math.Floor(WindowWidth / 640.0); }
        }

        public double WindowScaleY
        {
            get { return Math.Floor(WindowHeight / 480.0); }
        }

        public DateTime CreationTime
        {
            get { return creationTime; }
            set { SetProperty(ref creationTime, value); }
        }

        public ClientProcess() { }

        public void Update()
        {
            var windowTextLength = NativeMethods.GetWindowTextLength(windowHandle);
            var windowTextBuffer = new StringBuilder(windowTextLength + 1);
            windowTextLength = NativeMethods.GetWindowText(windowHandle, windowTextBuffer, windowTextBuffer.Capacity);

            WindowTitle = windowTextBuffer.ToString(0, windowTextLength);

            if (NativeMethods.GetWindowRect(windowHandle, out var windowRect))
            {
                WindowWidth = windowRect.Width;
                WindowHeight = windowRect.Height;
            }
        }
    }
}
