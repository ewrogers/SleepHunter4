using System;

namespace SleepHunter.IO.Process
{
    internal delegate void ClientProcessEventHandler(object sender, ClientProcessEventArgs e);

    internal sealed class ClientProcessEventArgs : EventArgs
    {
        private readonly ClientProcess process;

        public ClientProcess Process { get { return process; } }

        public ClientProcessEventArgs(ClientProcess process)
        {
            this.process = process;
        }
    }
}
