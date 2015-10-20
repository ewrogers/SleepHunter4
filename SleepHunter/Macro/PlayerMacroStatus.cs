using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SleepHunter.Macro
{
   public enum PlayerMacroStatus
   {
      Idle = 0,
      Waiting,
      WaitingForMana,
      WaitingOnVineyard,
      ReadyToFlower,
      UsingSkills,
      Assailing,
      SwitchingStaff,
      Disarming,
      Casting,
      FasSpiorad,
      Flowering,
      Vineyarding,
      Following,
      Walking,
      Thinking,
      ChatIsUp,
      Nothing = -1
   }
}
