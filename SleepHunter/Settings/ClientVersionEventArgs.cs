using System;

namespace SleepHunter.Settings
{
    public delegate void ClientVersionEventHandler(object sender, ClientVersionEventArgs e);

   public sealed class ClientVersionEventArgs : EventArgs
   {
      readonly ClientVersion version;

      public ClientVersion Version { get { return version; } }

      public ClientVersionEventArgs(ClientVersion version)
      {
         if (version == null)
            throw new ArgumentNullException("version");

         this.version = version;
      }
   }
}
