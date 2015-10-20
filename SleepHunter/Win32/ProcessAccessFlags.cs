using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SleepHunter.Win32
{
   [Flags]
   internal enum ProcessAccessFlags
   {
      None = 0x0,
      Terminate = 0x01,
      CreateThread = 0x2,
      VmOperation = 0x8,
      VmRead = 0x10,
      VmWrite = 0x20,
      QueryInformation = 0x400
   }
}
