using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SleepHunter.Models;

namespace SleepHunter.ViewModels
{
    public sealed class ClientListViewModel : IDisposable
    {
        private readonly ObservableCollection<
            ClientListItemViewModel> clients = new();
        private readonly ReadOnlyObservableCollection<
            ClientListItemViewModel> readOnlyClients;
        private bool isDisposed;

        public ClientListViewModel()
        {
            readOnlyClients = new ReadOnlyObservableCollection<
                ClientListItemViewModel>(clients);
        }

        public ReadOnlyObservableCollection<ClientListItemViewModel>
            Clients => readOnlyClients;

        public void Refresh(
            IEnumerable<Player> players,
            Func<int, ClientRuntimeViewModel> findRuntime)
        {
            ArgumentNullException.ThrowIfNull(players);
            ArgumentNullException.ThrowIfNull(findRuntime);
            ObjectDisposedException.ThrowIf(isDisposed, this);

            var desiredPlayers = players.ToArray();
            if (desiredPlayers.Any(player => player is null))
            {
                throw new ArgumentException(
                    "The client list cannot contain null players.",
                    nameof(players));
            }

            var processIds = desiredPlayers
                .Select(player => player.Process.ProcessId)
                .ToHashSet();
            if (processIds.Count != desiredPlayers.Length)
            {
                throw new ArgumentException(
                    "The client list cannot contain duplicate processes.",
                    nameof(players));
            }

            for (var index = clients.Count - 1; index >= 0; index--)
            {
                if (processIds.Contains(
                        clients[index].Process.ProcessId))
                {
                    continue;
                }

                var removed = clients[index];
                clients.RemoveAt(index);
                removed.Dispose();
            }

            for (var desiredIndex = 0;
                 desiredIndex < desiredPlayers.Length;
                 desiredIndex++)
            {
                var player = desiredPlayers[desiredIndex];
                var processId = player.Process.ProcessId;
                var item = clients.FirstOrDefault(
                    current =>
                        current.Process.ProcessId == processId);
                if (item is null)
                {
                    item = new ClientListItemViewModel(
                        player,
                        findRuntime(processId));
                    clients.Insert(desiredIndex, item);
                    continue;
                }

                if (!ReferenceEquals(item.Player, player))
                {
                    throw new InvalidOperationException(
                        $"Process {processId} changed player ownership without being removed.");
                }

                item.SetRuntime(findRuntime(processId));
                var currentIndex = clients.IndexOf(item);
                if (currentIndex != desiredIndex)
                    clients.Move(currentIndex, desiredIndex);
            }
        }

        public void Dispose()
        {
            if (isDisposed)
                return;

            foreach (var client in clients)
                client.Dispose();

            clients.Clear();
            isDisposed = true;
        }
    }
}
