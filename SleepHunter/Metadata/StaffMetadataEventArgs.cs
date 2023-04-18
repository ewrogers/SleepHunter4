using System;

namespace SleepHunter.Metadata
{
    internal delegate void StaffMetadataEventHandler(object sender, StaffMetadataEventArgs e);

    internal sealed class StaffMetadataEventArgs : EventArgs
    {
        private readonly StaffMetadata staff;

        public StaffMetadata Staff => staff;

        public StaffMetadataEventArgs(StaffMetadata staff)
        {
            this.staff = staff;
        }
    }
}
