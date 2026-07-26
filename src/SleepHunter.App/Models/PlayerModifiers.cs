using System;

using SleepHunter.Common;

namespace SleepHunter.Models
{
    public sealed class PlayerModifiers : UpdatableObject
    {
        public Player Owner { get; init; }

        public PlayerModifiers(Player owner)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        protected override void OnUpdate()
        {
            // Does nothing yet
        }
    }
}
