using System;

namespace SleepHunter.IO.Process
{
    public delegate void ClientProcessEventHandler(object sender, ClientProcessEventArgs e);

   public sealed class ClientProcessEventArgs : EventArgs
   {
      readonly ClientProcess process;

      public ClientProcess Process { get { return process; } }

      public ClientProcessEventArgs(ClientProcess process)
      {
         this.process = process;
      }
   }
}
