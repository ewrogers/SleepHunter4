using System;

namespace SleepHunter.Models
{
    internal delegate void PlayerEventHandler(object sender, PlayerEventArgs e);

    internal sealed class PlayerEventArgs : EventArgs
    {
        private readonly Player player;

        public Player Player => player;

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
