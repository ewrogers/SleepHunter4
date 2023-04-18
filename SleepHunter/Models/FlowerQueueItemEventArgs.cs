using System;

namespace SleepHunter.Models
{
    internal delegate void FlowerQueueItemEventHandler(object sender, FlowerQueueItemEventArgs e);

    internal sealed class FlowerQueueItemEventArgs : EventArgs
    {
        private readonly FlowerQueueItem flower;

        public FlowerQueueItem Flower => flower;

        public FlowerQueueItemEventArgs(FlowerQueueItem flower)
        {
            this.flower = flower;
        }
    }
}
