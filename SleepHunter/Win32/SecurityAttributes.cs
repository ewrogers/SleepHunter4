using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SleepHunter.Win32
{
   [StructLayout(LayoutKind.Sequential)]
   internal struct SecurityAttributes
   {
      int size;
      IntPtr securityDescriptor;
      bool inheritHandle;

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
