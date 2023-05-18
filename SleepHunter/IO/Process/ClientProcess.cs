using System;
using System.Text;

using SleepHunter.Common;
using SleepHunter.Win32;

namespace SleepHunter.IO.Process
{
    public sealed class ClientProcess : ObservableObject
    {
        int processId;
        IntPtr windowHandle;
        string windowClassName = string.Empty;
        string windowTitle = string.Empty;
        int windowWidth = 640;
        int windowHeight = 480;
        DateTime creationTime;

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
            set { SetProperty(ref windowWidth, value, onChanged: (p) => { RaisePropertyChanged(nameof(WindowScaleX)); }); }
        }

        public int WindowHeight
        {
            get { return windowHeight; }
            set { SetProperty(ref windowHeight, value, onChanged: (p) => { RaisePropertyChanged(nameof(WindowScaleY)); }); }
        }

        public double WindowScaleX
        {
            get { return WindowWidth / 640.0; }
        }

        public double WindowScaleY
        {
            get { return WindowHeight / 480.0; }
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

            Rect clientRect;

            if (NativeMethods.GetClientRect(windowHandle, out clientRect))
            {
                WindowWidth = clientRect.Width;
                WindowHeight = clientRect.Height;
            }
        }
    }
}
