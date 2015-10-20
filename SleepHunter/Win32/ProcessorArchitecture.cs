using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SleepHunter.Win32
{
   internal enum ProcessorArchitecture : short
   {
      Unknown = -1,
      X86 = 0,
      Itanium64 = 6,
      X64 = 9
   }
}
