using System;
using System.Diagnostics;
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

        public ClientProcess() { }

        public void Update()
        {
            Debug.WriteLine($"Updating client window state (pid={ProcessId}, hwnd={WindowHandle})...");

            var windowTextLength = NativeMethods.GetWindowTextLength(windowHandle);
            var windowTextBuffer = new StringBuilder(windowTextLength + 1);
            windowTextLength = NativeMethods.GetWindowText(windowHandle, windowTextBuffer, windowTextBuffer.Capacity);

            WindowTitle = windowTextBuffer.ToString(0, windowTextLength);

            Rect windowRect;

            if (NativeMethods.GetWindowRect(windowHandle, out windowRect))
            {
                WindowWidth = windowRect.Width;
                WindowHeight = windowRect.Height;
            }

            Debug.WriteLine($"WindowTitle = {WindowTitle}");
            Debug.WriteLine($"WindowWidth = {WindowWidth}");
            Debug.WriteLine($"WindowHeight = {WindowHeight}");
            Debug.WriteLine($"WindowScaleX = {WindowScaleX}");
            Debug.WriteLine($"WindowScaleY = {WindowScaleY}");
        }
    }
}
