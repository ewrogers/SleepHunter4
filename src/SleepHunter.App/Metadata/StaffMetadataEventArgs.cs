using System;

namespace SleepHunter.Metadata
{
    public delegate void StaffMetadataEventHandler(object sender, StaffMetadataEventArgs e);

    public sealed class StaffMetadataEventArgs : EventArgs
    {
        public StaffMetadata Staff { get; }

        public StaffMetadataEventArgs(StaffMetadata staff)
        {
            Staff = staff ?? throw new ArgumentNullException(nameof(staff));
        }
    }
}
