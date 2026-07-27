using System;

namespace SleepHunter.ViewModels
{
    public sealed class ClientLoginStateChangedEventArgs :
        EventArgs
    {
        public ClientLoginStateChangedEventArgs(
            ClientListItemViewModel client,
            bool isLoggedIn)
        {
            Client = client ??
                throw new ArgumentNullException(nameof(client));
            IsLoggedIn = isLoggedIn;
        }

        public ClientListItemViewModel Client { get; }

        public bool IsLoggedIn { get; }
    }
}
