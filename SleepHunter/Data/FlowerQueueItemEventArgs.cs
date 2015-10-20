using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SleepHunter.Data
{
   public delegate void FlowerQueueItemEventHandler(object sender, FlowerQueueItemEventArgs e);

   public sealed class FlowerQueueItemEventArgs : EventArgs
   {
      readonly FlowerQueueItem flower;

      public FlowerQueueItem Flower { get { return flower; } }

      public FlowerQueueItemEventArgs(FlowerQueueItem flower)
      {
         this.flower = flower;
      }
   }
}
