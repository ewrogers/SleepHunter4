using System;

using SleepHunter.Common;
using SleepHunter.IO.Process;

namespace SleepHunter.Models
{
    public sealed class PlayerModifiers : ObservableObject
    {
        public event EventHandler ModifiersUpdated;

        public Player Owner { get; }

        public PlayerModifiers(Player owner)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        public void Update()
        {
            Update(Owner.Accessor);
            ModifiersUpdated?.Invoke(this, EventArgs.Empty);
        }

        public void Update(ProcessMemoryAccessor accessor)
        {
            if (accessor == null)
                throw new ArgumentNullException(nameof(accessor));

            // Not currently implemented
        }
    }
}
