using System;
using System.Runtime.InteropServices;

namespace SleepHunter.Win32
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct Rect
    {
        private int left;
        private int top;
        private int right;
        private int bottom;

        public int Left
        {
            readonly get => left;
            set => left = value;
        }

        public int Top
        {
            readonly get => top;
            set => top = value;
        }

        public int Right
        {
            readonly get => right;
            set => right = value;
        }

        public int Bottom
        {
            readonly get => bottom;
            set => bottom = value;
        }

        public readonly int Width => Math.Abs(right - left);

        public readonly int Height => Math.Abs(bottom - top);
    }
}
