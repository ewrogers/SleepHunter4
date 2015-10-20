using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SleepHunter.Win32;

namespace SleepHunter.IO.Process
{
   [Flags]
   public enum ProcessAccess
   {
      Read = 0x1,
      Write = 0x2,
      ReadWrite = Read | Write
   }

   public static class ProcessAccessExtender
   {
      internal static ProcessAccessFlags ToWin32Flags(this ProcessAccess access)
      {
         var flags = ProcessAccessFlags.VmOperation | ProcessAccessFlags.QueryInformation;

         if (access.HasFlag(ProcessAccess.Read))
            flags |= ProcessAccessFlags.VmRead;

         if (access.HasFlag(ProcessAccess.Write))
            flags |= ProcessAccessFlags.VmWrite;

         return flags;
      }
   }
}
