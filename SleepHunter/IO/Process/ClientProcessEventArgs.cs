using System;

namespace SleepHunter.IO.Process
{
    public delegate void ClientProcessEventHandler(object sender, ClientProcessEventArgs e);

    public sealed class ClientProcessEventArgs : EventArgs
    {
        public ClientProcess Process { get; }

        public ClientProcessEventArgs(ClientProcess process)
            => Process = process ?? throw new ArgumentNullException(nameof(process));
    }
}
