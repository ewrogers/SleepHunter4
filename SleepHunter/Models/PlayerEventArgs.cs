using System;

namespace SleepHunter.Models
{
    public delegate void PlayerEventHandler(object sender, PlayerEventArgs e);

    public sealed class PlayerEventArgs : EventArgs
    {
        public Player Player { get; }

        public PlayerEventArgs(Player player)
            => Player = player ?? throw new ArgumentNullException(nameof(player));

        public override string ToString() => Player.Name;
    }
}
