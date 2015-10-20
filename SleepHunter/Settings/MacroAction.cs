using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SleepHunter.Settings
{
   public enum MacroAction
   {
      None = 0,
      Start,
      Resume,
      Restart,
      Pause,
      Stop,
      ForceQuit
   }
}
