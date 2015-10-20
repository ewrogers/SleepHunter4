using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SleepHunter.Updates
{
   public delegate void SoftwareUpdateEventHandler(object sender, SoftwareUpdateEventArgs e);

   public class SoftwareUpdateEventArgs : EventArgs
   {
      readonly Uri uri;
      readonly string filename;
      readonly long bytesReceived;
      readonly long totalBytesToReceive;

      public Uri Uri { get { return uri; } }
      public string Filename { get { return filename; } }
      public long BytesReceived { get { return bytesReceived; } }
      public long TotalBytesToReceive { get { return totalBytesToReceive; } }
      public double PercentCompleted
      {
         get
         {
            if (totalBytesToReceive <= 0)
               return 0;

            return bytesReceived * 100.0 / totalBytesToReceive;
         }
      }

      public SoftwareUpdateEventArgs(Uri uri, string filename, long bytesReceived, long totalBytesToReceive)
      {
         this.uri = uri;
         this.filename = filename;
         this.bytesReceived = bytesReceived;
         this.totalBytesToReceive = totalBytesToReceive;
      }
   }
}
