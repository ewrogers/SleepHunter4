using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SleepHunter.Win32
{
   [Flags]
   internal enum VirtualMemoryStatus : uint
   {
      None = 0,
      Commit = 0x1000,
      Free = 0x10000,
      Reserve = 0x2000
   }
}
