using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SleepHunter.Data
{
   public delegate void PlayerEventHandler(object sender, PlayerEventArgs e);

   public sealed class PlayerEventArgs : EventArgs
   {
      readonly Player player;

      public Player Player { get { return player; } }

      public PlayerEventArgs(Player player)
      {
         this.player = player;
      }

      public override string ToString()
      {
         return player.Name;
      }
   }
}
