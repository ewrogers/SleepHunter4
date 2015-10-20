using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SleepHunter.Metadata
{
   public delegate void StaffMetadataEventHandler(object sender, StaffMetadataEventArgs e);

   public sealed class StaffMetadataEventArgs :EventArgs
   {
      readonly StaffMetadata staff;

      public StaffMetadata Staff { get { return staff; } }

      public StaffMetadataEventArgs(StaffMetadata staff)
      {
         this.staff = staff;
      }
   }
}
