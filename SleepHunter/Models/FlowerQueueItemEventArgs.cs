using System;

namespace SleepHunter.Models
{
    public delegate void FlowerQueueItemEventHandler(object sender, FlowerQueueItemEventArgs e);

    public sealed class FlowerQueueItemEventArgs : EventArgs
    {
        public FlowerQueueItem Flower { get; }

        public FlowerQueueItemEventArgs(FlowerQueueItem flower)
        {
            Flower = flower ?? throw new ArgumentNullException(nameof(flower));
        }
    }
}
