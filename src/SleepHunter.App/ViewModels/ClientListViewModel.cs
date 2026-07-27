using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SleepHunter.Models;
using SleepHunter.Services.Configuration;
using SleepHunter.Services.Hotkeys;
using SleepHunter.Services.Logging;

namespace SleepHunter.ViewModels
{
    public sealed partial class ClientListViewModel :
        ObservableObject,
        IDisposable
    {
        private readonly ObservableCollection<
            ClientListItemViewModel> clients = new();
        private readonly Dictionary<
            int,
            ClientListItemViewModel> allClients = new();
        private readonly Func<
            ClientSession,
            ClientRuntimeViewModel,
            ClientListItemViewModel> createItem;
        private readonly Dictionary<int, bool> loginStates = new();
        private readonly ReadOnlyObservableCollection<
            ClientListItemViewModel> readOnlyClients;
        private ClientSortOrder sortOrder =
            ClientSortOrder.LaunchOrder;
        private bool showAllClients = true;
        private bool isDisposed;

        public ClientListViewModel()
            : this(
                (session, runtime) =>
                    new ClientListItemViewModel(session, runtime))
        {
        }

        internal ClientListViewModel(
            Func<
                ClientSession,
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

        public event EventHandler<ClientLoginStateChangedEventArgs>
            ClientLoginStateChanged;

        public IReadOnlyList<ClientListItemViewModel> AllClients =>
            allClients.Values
                .OrderBy(client => client.Process.CreationTime)
                .ThenBy(client => client.Process.ProcessId)
                .ToArray();

        public IReadOnlyList<ClientListItemViewModel> LoggedInClients =>
            allClients.Values
                .Where(client => client.IsLoggedIn)
                .OrderBy(client => client.Name)
                .ThenBy(client => client.Process.ProcessId)
                .ToArray();

        public bool HasLoggedInClients =>
            allClients.Values.Any(client => client.IsLoggedIn);

        public string WindowTitle =>
            SelectedClient is
            {
                IsLoggedIn: true,
                Name: { Length: > 0 } name
            }
                ? $"SleepHunter - {name}"
                : "SleepHunter";

        [ObservableProperty]
        public partial ClientListItemViewModel SelectedClient
        {
            get;
            set;
        }

        public void Refresh(
            IEnumerable<ClientSession> sessions,
            Func<int, ClientRuntimeViewModel> findRuntime,
            ClientSortOrder sortOrder = ClientSortOrder.LaunchOrder,
            bool showAllClients = true)
        {
            ArgumentNullException.ThrowIfNull(sessions);
            ArgumentNullException.ThrowIfNull(findRuntime);
            ObjectDisposedException.ThrowIf(isDisposed, this);

            var desiredSessions = sessions.ToArray();
            if (desiredSessions.Any(session => session is null))
            {
                throw new ArgumentException(
                    "The client list cannot contain null sessions.",
                    nameof(sessions));
            }

            var processIds = desiredSessions
                .Select(session => session.Process.ProcessId)
                .ToHashSet();
            if (processIds.Count != desiredSessions.Length)
            {
                throw new ArgumentException(
                    "The client list cannot contain duplicate processes.",
                    nameof(sessions));
            }

            this.sortOrder = sortOrder;
            this.showAllClients = showAllClients;

            foreach (var processId in allClients.Keys
                         .Where(processId =>
                             !processIds.Contains(processId))
                         .ToArray())
            {
                var removed = allClients[processId];
                if (ReferenceEquals(SelectedClient, removed))
                    SelectedClient = null;

                removed.PropertyChanged -= OnClientPropertyChanged;
                clients.Remove(removed);
                allClients.Remove(processId);
                loginStates.Remove(processId);
                removed.Dispose();
            }

            foreach (var session in desiredSessions)
            {
                var processId = session.Process.ProcessId;
                if (!allClients.TryGetValue(
                        processId,
                        out var item))
                {
                    item = createItem(
                        session,
                        findRuntime(processId)) ??
                        throw new InvalidOperationException(
                            "The client-list item factory returned no item.");
                    item.PropertyChanged += OnClientPropertyChanged;
                    allClients.Add(processId, item);
                    loginStates.Add(processId, false);
                    ObserveLoginState(item);
                    continue;
                }

                if (!ReferenceEquals(item.Session, session))
                {
                    throw new InvalidOperationException(
                        $"Process {processId} changed session ownership without being removed.");
                }

                item.SetRuntime(findRuntime(processId));
            }

            RebuildVisibleClients();
            StopAllMacrosCommand.NotifyCanExecuteChanged();
        }

        public ClientListItemViewModel FindByProcessId(
            int processId) =>
            allClients.GetValueOrDefault(processId);

        public ClientListItemViewModel FindByHotkey(Hotkey hotkey)
        {
            if (hotkey is null)
                return null;

            return allClients.Values.FirstOrDefault(
                client =>
                    client.Session.Hotkey is { } assigned &&
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

            foreach (var client in allClients.Values)
            {
                client.PropertyChanged -= OnClientPropertyChanged;
                client.Dispose();
            }

            clients.Clear();
            allClients.Clear();
            loginStates.Clear();
            isDisposed = true;
        }

        [RelayCommand(CanExecute = nameof(CanStopAllMacros))]
        private async Task StopAllMacrosAsync(
            CancellationToken cancellationToken)
        {
            foreach (var client in allClients.Values.ToArray())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (client.StopMacroCommand.CanExecute(null))
                    await client.StopMacroCommand.ExecuteAsync(null);
            }
        }

        private bool CanStopAllMacros() =>
            !isDisposed &&
            allClients.Values.Any(
                client =>
                    client.StopMacroCommand.CanExecute(null));

        private void OnClientPropertyChanged(
            object sender,
            PropertyChangedEventArgs e)
        {
            MacroPersistence?.NotifyStateChanged();

            if (e.PropertyName is null ||
                string.Equals(
                    e.PropertyName,
                    nameof(ClientListItemViewModel.IsLoggedIn),
                    StringComparison.Ordinal) ||
                string.Equals(
                    e.PropertyName,
                    nameof(ClientListItemViewModel.Name),
                    StringComparison.Ordinal) ||
                string.Equals(
                    e.PropertyName,
                    nameof(ClientListItemViewModel.MaximumHealth),
                    StringComparison.Ordinal) ||
                string.Equals(
                    e.PropertyName,
                    nameof(ClientListItemViewModel.MaximumMana),
                    StringComparison.Ordinal))
            {
                ObserveLoginState((ClientListItemViewModel)sender);
                RebuildVisibleClients();
            }

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

            if (e.PropertyName is null ||
                string.Equals(
                    e.PropertyName,
                    nameof(ClientListItemViewModel.IsLoggedIn),
                    StringComparison.Ordinal) ||
                string.Equals(
                    e.PropertyName,
                    nameof(ClientListItemViewModel.Name),
                    StringComparison.Ordinal))
            {
                OnPropertyChanged(nameof(WindowTitle));
            }
        }

        partial void OnSelectedClientChanging(
            ClientListItemViewModel value)
        {
            if (value is not null &&
                !allClients.Values.Contains(value))
            {
                throw new ArgumentException(
                    "The selected client must belong to the client list.",
                    nameof(value));
            }
        }

        partial void OnSelectedClientChanged(
            ClientListItemViewModel value)
        {
            foreach (var client in allClients.Values)
                client.IsRuntimeDetailsOpen = false;

            MacroPersistence?.NotifyStateChanged();
            OnPropertyChanged(nameof(WindowTitle));
        }

        private void ObserveLoginState(
            ClientListItemViewModel client)
        {
            var processId = client.Process.ProcessId;
            var isLoggedIn = client.IsLoggedIn;
            if (!loginStates.TryGetValue(
                    processId,
                    out var wasLoggedIn) ||
                wasLoggedIn == isLoggedIn)
            {
                return;
            }

            loginStates[processId] = isLoggedIn;
            OnPropertyChanged(nameof(HasLoggedInClients));
            OnPropertyChanged(nameof(LoggedInClients));
            ClientLoginStateChanged?.Invoke(
                this,
                new ClientLoginStateChangedEventArgs(
                    client,
                    isLoggedIn));
        }

        private void RebuildVisibleClients()
        {
            var desired = SortClients(
                    allClients.Values.Where(client =>
                        client.IsLoggedIn),
                    sortOrder)
                .ToList();
            if (showAllClients)
            {
                desired.AddRange(
                    allClients.Values
                        .Where(client => !client.IsLoggedIn)
                        .OrderBy(
                            client =>
                                client.Process.CreationTime)
                        .ThenBy(
                            client =>
                                client.Process.ProcessId));
            }

            var desiredSet = desired.ToHashSet();
            for (var index = clients.Count - 1;
                 index >= 0;
                 index--)
            {
                if (!desiredSet.Contains(clients[index]))
                    clients.RemoveAt(index);
            }

            for (var desiredIndex = 0;
                 desiredIndex < desired.Count;
                 desiredIndex++)
            {
                var client = desired[desiredIndex];
                var currentIndex = clients.IndexOf(client);
                if (currentIndex < 0)
                    clients.Insert(desiredIndex, client);
                else if (currentIndex != desiredIndex)
                    clients.Move(currentIndex, desiredIndex);
            }

            if (SelectedClient is not null &&
                !clients.Contains(SelectedClient))
            {
                SelectedClient = null;
            }
        }

        internal static IEnumerable<ClientListItemViewModel>
            SortClients(
                IEnumerable<ClientListItemViewModel> source,
                ClientSortOrder sortOrder)
        {
            ArgumentNullException.ThrowIfNull(source);

            return sortOrder switch
            {
                ClientSortOrder.LaunchOrder =>
                    source.OrderBy(
                            client =>
                                client.Process.CreationTime)
                        .ThenBy(
                            client =>
                                client.Process.ProcessId),
                ClientSortOrder.Alphabetical =>
                    source.OrderBy(client => client.Name)
                        .ThenBy(
                            client =>
                                client.Process.ProcessId),
                ClientSortOrder.HighestHealth =>
                    source.OrderByDescending(
                            client =>
                                client.MaximumHealth)
                        .ThenBy(client => client.Name),
                ClientSortOrder.HighestMana =>
                    source.OrderByDescending(
                            client =>
                                client.MaximumMana)
                        .ThenBy(client => client.Name),
                ClientSortOrder.HighestCombined =>
                    source.OrderByDescending(
                            client =>
                                client.MaximumHealth +
                                (client.MaximumMana * 2))
                        .ThenBy(client => client.Name),
                _ => source.OrderBy(client => client.Name)
            };
        }
    }
}
