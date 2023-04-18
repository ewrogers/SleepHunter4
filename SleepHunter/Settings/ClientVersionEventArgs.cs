using System;

namespace SleepHunter.Settings
{
    internal delegate void ClientVersionEventHandler(object sender, ClientVersionEventArgs e);

    internal sealed class ClientVersionEventArgs : EventArgs
    {
        private readonly ClientVersion version;

        public ClientVersion Version { get { return version; } }

        public ClientVersionEventArgs(ClientVersion version)
        {
            this.version = version ?? throw new ArgumentNullException(nameof(version));
        }
    }
}
