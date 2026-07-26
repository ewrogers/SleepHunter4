using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SleepHunter.Macro;
using SleepHunter.Models;
using SleepHunter.Services.Configuration;
using SleepHunter.Services.Logging;

namespace SleepHunter.ViewModels
{
    public sealed partial class ClientListViewModel :
        ObservableObject,
        IDisposable
    {
        private readonly ObservableCollection<
            ClientListItemViewModel> clients = new();
        private readonly Func<
            Player,
            ClientRuntimeViewModel,
            ClientListItemViewModel> createItem;
        private readonly ReadOnlyObservableCollection<
            ClientListItemViewModel> readOnlyClients;
        private bool isDisposed;

        public ClientListViewModel()
            : this(
                (player, runtime) =>
                    new ClientListItemViewModel(player, runtime))
        {
        }

        internal ClientListViewModel(
            Func<
                Player,
                ClientRuntimeViewModel,
                ClientListItemViewModel> createItem,
            IMacroConfigurationPersistenceService
                macroPersistence = null,
            IMacroConfigurationInteraction
                macroInteraction = null,
            ILogger logger = null,
            ClientLaunchViewModel clientLaunch = null)
        {
            this.createItem = createItem ??
                throw new ArgumentNullException(nameof(createItem));
            readOnlyClients = new ReadOnlyObservableCollection<
                ClientListItemViewModel>(clients);
            if (macroPersistence is not null &&
                macroInteraction is not null &&
                logger is not null)
            {
                MacroPersistence = new MacroPersistenceViewModel(
                    () => SelectedClient,
                    macroPersistence,
                    macroInteraction,
                    logger);
            }

            ClientLaunch = clientLaunch;
        }

        public ReadOnlyObservableCollection<ClientListItemViewModel>
            Clients => readOnlyClients;

        public MacroPersistenceViewModel MacroPersistence { get; }

        public ClientLaunchViewModel ClientLaunch { get; }

        [ObservableProperty]
        public partial ClientListItemViewModel SelectedClient
        {
            get;
            set;
        }

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
                if (ReferenceEquals(SelectedClient, removed))
                    SelectedClient = null;

                removed.PropertyChanged -= OnClientPropertyChanged;
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
                    item = createItem(
                        player,
                        findRuntime(processId)) ??
                        throw new InvalidOperationException(
                            "The client-list item factory returned no item.");
                    item.PropertyChanged += OnClientPropertyChanged;
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

            StopAllMacrosCommand.NotifyCanExecuteChanged();
        }

        public ClientListItemViewModel FindByHotkey(Hotkey hotkey)
        {
            if (hotkey is null)
                return null;

            return clients.FirstOrDefault(
                client =>
                    client.Player.Hotkey is { } assigned &&
                    assigned.Key == hotkey.Key &&
                    assigned.Modifiers == hotkey.Modifiers);
        }

        public void Dispose()
        {
            if (isDisposed)
                return;

            StopAllMacrosCommand.Cancel();
            MacroPersistence?.Dispose();
            SelectedClient = null;

            foreach (var client in clients)
            {
                client.PropertyChanged -= OnClientPropertyChanged;
                client.Dispose();
            }

            clients.Clear();
            isDisposed = true;
        }

        [RelayCommand(CanExecute = nameof(CanStopAllMacros))]
        private async Task StopAllMacrosAsync(
            CancellationToken cancellationToken)
        {
            foreach (var client in clients.ToArray())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (client.StopMacroCommand.CanExecute(null))
                    await client.StopMacroCommand.ExecuteAsync(null);
            }
        }

        private bool CanStopAllMacros() =>
            !isDisposed &&
            clients.Any(
                client =>
                    client.StopMacroCommand.CanExecute(null));

        private void OnClientPropertyChanged(
            object sender,
            PropertyChangedEventArgs e)
        {
            MacroPersistence?.NotifyStateChanged();

            if (string.Equals(
                    e.PropertyName,
                    nameof(ClientListItemViewModel.IsMacroRunning),
                    StringComparison.Ordinal) ||
                string.Equals(
                    e.PropertyName,
                    nameof(ClientListItemViewModel.IsMacroPaused),
                    StringComparison.Ordinal) ||
                string.Equals(
                    e.PropertyName,
                    nameof(ClientListItemViewModel.IsAutomationCommandRunning),
                    StringComparison.Ordinal))
            {
                StopAllMacrosCommand.NotifyCanExecuteChanged();
            }
        }

        partial void OnSelectedClientChanging(
            ClientListItemViewModel value)
        {
            if (value is not null && !clients.Contains(value))
            {
                throw new ArgumentException(
                    "The selected client must belong to the client list.",
                    nameof(value));
            }
        }

        partial void OnSelectedClientChanged(
            ClientListItemViewModel value)
        {
            foreach (var client in clients)
                client.IsRuntimeDetailsOpen = false;

            MacroPersistence?.NotifyStateChanged();
        }
    }
}
