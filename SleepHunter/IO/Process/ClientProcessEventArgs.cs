using System;

namespace SleepHunter.IO.Process
{
    internal delegate void ClientProcessEventHandler(object sender, ClientProcessEventArgs e);

    internal sealed class ClientProcessEventArgs : EventArgs
    {
        public ClientProcess Process { get; }

        public ClientProcessEventArgs(ClientProcess process)
            => Process = process ?? throw new ArgumentNullException(nameof(process));
    }
}
