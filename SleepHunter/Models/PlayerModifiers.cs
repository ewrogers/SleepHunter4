using System;

using SleepHunter.Common;
using SleepHunter.IO.Process;

namespace SleepHunter.Models
{
    public sealed class PlayerModifiers : ObservableObject
    {
        Player owner;

        public Player Owner
        {
            get => owner;
            set => SetProperty(ref owner, value);
        }

        public PlayerModifiers(Player owner)
            => this.owner = owner ?? throw new ArgumentNullException(nameof(owner));

        public void Update(ProcessMemoryAccessor accessor)
        {
            if (accessor == null)
                throw new ArgumentNullException(nameof(accessor));
        }
    }
}
