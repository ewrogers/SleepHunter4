using System.Runtime.InteropServices;

namespace SleepHunter.Win32
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct SecurityAttributes
    {
        private int size;
        private nint securityDescriptor;
        private bool inheritHandle;

        public int Size
        {
            readonly get => size;
            set => size = value;
        }

        public nint SecurityDescriptor
        {
            readonly get => securityDescriptor;
            set => securityDescriptor = value;
        }

        public bool InheritHandle
        {
            readonly get => inheritHandle;
            set => inheritHandle = value;
        }
    }
}
