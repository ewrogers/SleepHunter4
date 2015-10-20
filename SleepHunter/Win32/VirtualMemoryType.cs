using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SleepHunter.Win32
{
   [Flags]
   internal enum VirtualMemoryType : uint
   {
      None = 0,
      Image = 0x1000000,
      Mapped = 0x40000,
      Private = 0x20000,
   }
}
