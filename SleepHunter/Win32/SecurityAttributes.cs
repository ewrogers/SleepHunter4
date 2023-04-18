using System;
using System.Runtime.InteropServices;

namespace SleepHunter.Win32
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct SecurityAttributes
    {
        private int size;
        private IntPtr securityDescriptor;
        private bool inheritHandle;

        public int Size
        {
            get { return size; }
            set { size = value; }
        }

        public IntPtr SecurityDescriptor
        {
            get { return securityDescriptor; }
            set { securityDescriptor = value; }
        }

        public bool InheritHandle
        {
            get { return inheritHandle; }
            set { inheritHandle = value; }
        }
    }
}
