using System;
using SleepHunter.Models;

namespace SleepHunter.Services.Clients
{
    public sealed class ClientSessionEventArgs : EventArgs
    {
        public ClientSession Session { get; }

        public ClientSessionEventArgs(ClientSession session)
        {
            Session = session ??
                throw new ArgumentNullException(nameof(session));
        }

        public override string ToString() =>
            Session.Process.ProcessId.ToString();
    }
}
