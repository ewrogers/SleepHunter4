using System;

using SleepHunter.Common;
using SleepHunter.IO.Process;

namespace SleepHunter.Models
{
    public sealed class PlayerModifiers : ObservableObject
    {
        Player owner;

        public event EventHandler ModifiersUpdated;

        public Player Owner
        {
            get { return owner; }
            set { SetProperty(ref owner, value); }
        }

        public PlayerModifiers()
           : this(null) { }

        public PlayerModifiers(Player owner)
        {
            this.owner = owner;
        }

        public void Update()
        {
            if (owner == null)
                throw new InvalidOperationException("Player owner is null, cannot update.");

            Update(owner.Accessor);
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
