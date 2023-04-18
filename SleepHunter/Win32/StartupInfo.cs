using System;
using System.Runtime.InteropServices;

namespace SleepHunter.Win32
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct StartupInfo
    {
        private int size;
        private string reserved;
        private string desktop;
        private string title;
        private int x;
        private int y;
        private int width;
        private int height;
        private int consoleWidth;
        private int consoleHeight;
        private int fillAttribute;
        private int flags;
        private short showWindow;
        private short reserved2;
        private IntPtr reserved3;
        private IntPtr standardInput;
        private IntPtr standardOutput;
        private IntPtr standardError;

        public int Size
        {
            get { return size; }
            set { size = value; }
        }

        public string Reserved { get { return reserved; } }

        public string Desktop
        {
            get { return desktop; }
            set { desktop = value; }
        }

        public string Title
        {
            get { return title; }
            set { title = value; }
        }

        public int X
        {
            get { return x; }
            set { x = value; }
        }

        public int Y
        {
            get { return y; }
            set { y = value; }
        }

        public int Width
        {
            get { return width; }
            set { width = value; }
        }

        public int Height
        {
            get { return height; }
            set { height = value; }
        }

        public int ConsoleWidth
        {
            get { return consoleWidth; }
            set { consoleWidth = value; }
        }

        public int ConsoleHeight
        {
            get { return consoleHeight; }
            set { consoleHeight = value; }
        }

        public int FillAttribute
        {
            get { return fillAttribute; }
            set { fillAttribute = value; }
        }

        public int Flags
        {
            get { return flags; }
            set { flags = value; }
        }

        public short ShowWindow
        {
            get { return showWindow; }
            set { showWindow = value; }
        }

        public short Reserved2 { get { return reserved2; } }

        public IntPtr Reserved3 { get { return reserved3; } }

        public IntPtr StandardInput
        {
            get { return standardInput; }
            set { standardInput = value; }
        }

        public IntPtr StandardOutput
        {
            get { return standardOutput; }
            set { standardOutput = value; }
        }

        public IntPtr StandardError
        {
            get { return standardError; }
            set { standardError = value; }
        }
    }
}
