using System;

namespace SleepHunter.Models
{
    public delegate void ClientProcessEventHandler(object sender, ClientProcessEventArgs e);

    public sealed class ClientProcessEventArgs : EventArgs
    {
        public ClientProcess Process { get; init; }

        public ClientProcessEventArgs(ClientProcess process)
        {
            Process = process;
        }
    }
}
