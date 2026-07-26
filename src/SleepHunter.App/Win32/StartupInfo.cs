using System;
using System.Runtime.InteropServices;

namespace SleepHunter.Win32
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct StartupInfo
    {
        private int size;
        private readonly string reserved;
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
        private readonly short reserved2;
        private readonly nint reserved3;
        private nint standardInput;
        private nint standardOutput;
        private nint standardError;

        public int Size
        {
            readonly get => size;
            set => size = value;
        }

        public readonly string Reserved => reserved;

        public string Desktop
        {
            readonly get => desktop;
            set => desktop = value;
        }

        public string Title
        {
            readonly get => title;
            set => title = value;
        }

        public int X
        {
            readonly get => x;
            set => x = value;
        }

        public int Y
        {
            readonly get => y;
            set => y = value;
        }

        public int Width
        {
            readonly get => width;
            set => width = value;
        }

        public int Height
        {
            readonly get => height;
            set => height = value;
        }

        public int ConsoleWidth
        {
            readonly get => consoleWidth;
            set => consoleWidth = value;
        }

        public int ConsoleHeight
        {
            readonly get => consoleHeight;
            set => consoleHeight = value;
        }

        public int FillAttribute
        {
            readonly get => fillAttribute;
            set => fillAttribute = value;
        }

        public int Flags
        {
            readonly get => flags;
            set => flags = value;
        }

        public short ShowWindow
        {
            readonly get => showWindow;
            set => showWindow = value;
        }

        public readonly short Reserved2 => reserved2;

        public readonly nint Reserved3 => reserved3;

        public nint StandardInput
        {
            readonly get => standardInput;
            set => standardInput = value;
        }

        public nint StandardOutput
        {
            readonly get => standardOutput;
            set => standardOutput = value;
        }

        public nint StandardError
        {
            readonly get => standardError;
            set => standardError = value;
        }
    }
}
