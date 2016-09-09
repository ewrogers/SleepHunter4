using System;

namespace SleepHunter.Models
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
