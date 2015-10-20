using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SleepHunter.Win32
{
   internal enum ProcessorType :  uint
   {
      None = 0,
      Intel386 = 386,
      Intel486 = 486,
      IntelPentium = 586,
      Itanium64 = 2200,
      X64 = 8664
   }
}
