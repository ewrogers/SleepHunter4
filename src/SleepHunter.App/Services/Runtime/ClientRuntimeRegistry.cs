using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SleepHunter.Interop.Hosting;
using SleepHunter.Interop.Snapshots;
using SleepHunter.Runtime.Automation.Flowering;
using SleepHunter.Runtime.Automation.Skills;
using SleepHunter.Runtime.Automation.Spells;
using SleepHunter.Runtime.Engine;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Time;
using SleepHunter.Services.Logging;
using SleepHunter.ViewModels;

namespace SleepHunter.Services.Runtime
{
    public sealed class ClientRuntimeRegistry : IAsyncDisposable
    {
        private readonly ConcurrentDictionary<
            int,
            ClientRuntimeViewModel> clients = new();
        private readonly Func<AbilitySnapshotCatalog> abilityCatalog;
        private readonly MacroClock clock;
        private readonly IClientRuntimeFactory factory;
        private readonly ILogger logger;
        private readonly string mappingPath;
        private readonly object rosterGate = new();
        private readonly TimeProvider timeProvider;
        private readonly IUiDispatcher uiDispatcher;

        private int disposeState;
        private ImmutableArray<ClientRosterEntry> lastRoster = [];
        private long rosterSequence;

        public ClientRuntimeRegistry(
            IClientRuntimeFactory factory,
            ILogger logger,
            IUiDispatcher uiDispatcher,
            string mappingPath,
            TimeProvider timeProvider,
            Func<AbilitySnapshotCatalog> abilityCatalog)
        {
            this.factory = factory ??
                throw new ArgumentNullException(nameof(factory));
            this.logger = logger ??
                throw new ArgumentNullException(nameof(logger));
            this.uiDispatcher = uiDispatcher ??
                throw new ArgumentNullException(nameof(uiDispatcher));
            ArgumentException.ThrowIfNullOrWhiteSpace(mappingPath);
            this.mappingPath = Path.GetFullPath(mappingPath);
            this.timeProvider = timeProvider ??
                throw new ArgumentNullException(nameof(timeProvider));
            clock = new MacroClock(timeProvider);
            this.abilityCatalog = abilityCatalog ??
                throw new ArgumentNullException(nameof(abilityCatalog));
        }

        public int Count => clients.Count;

        public IReadOnlyCollection<ClientRuntimeViewModel> Clients =>
            clients.Values.ToArray();

        public async ValueTask<bool> AttachAsync(
            ClientRuntimeDescriptor descriptor,
            TimeSpan snapshotInterval,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(descriptor);
            ThrowIfDisposing();

            if (clients.ContainsKey(descriptor.ProcessId))
                return false;

            IClientRuntimeHost host;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var mappingStream = File.OpenRead(mappingPath);
                var catalog = abilityCatalog() ??
                    throw new InvalidOperationException(
                        "The runtime ability catalog is unavailable.");
                host = factory.Attach(
                    mappingStream,
                    descriptor.Client,
                    descriptor.ProcessId,
                    descriptor.WindowHandle,
                    new SnapshotCaptureSchedule(
                        snapshotInterval,
                        SnapshotCaptureSections.All),
                    clock,
                    catalog);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    $"Unable to attach runtime to process {descriptor.ProcessId}.");
                logger.LogException(exception);
                return false;
            }

            var viewModel = new ClientRuntimeViewModel(
                host,
                uiDispatcher);
            if (cancellationToken.IsCancellationRequested)
            {
                await viewModel.DisposeAsync().ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
            }

            if (!clients.TryAdd(descriptor.ProcessId, viewModel))
            {
                await viewModel.DisposeAsync().ConfigureAwait(false);
                return false;
            }

            viewModel.PropertyChanged += OnRuntimePropertyChanged;
            if (Volatile.Read(ref disposeState) != 0)
            {
                if (clients.TryRemove(
                        descriptor.ProcessId,
                        out var attachedRuntime))
                {
                    attachedRuntime.PropertyChanged -=
                        OnRuntimePropertyChanged;
                    await attachedRuntime
                        .DisposeAsync()
                        .ConfigureAwait(false);
                }

                ThrowIfDisposing();
            }

            logger.LogInfo(
                $"Attached active runtime to process {descriptor.ProcessId}.");
            PublishRoster();
            return true;
        }

        public bool TryFind(
            int processId,
            out ClientRuntimeViewModel viewModel) =>
            TryFindRuntime(processId, out viewModel);

        public async ValueTask<bool> DetachAsync(int processId)
        {
            if (!clients.TryRemove(processId, out var runtime))
                return false;

            runtime.PropertyChanged -= OnRuntimePropertyChanged;
            try
            {
                await runtime.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                logger.LogError(
                    $"Runtime shutdown failed for process {processId}.");
                logger.LogException(exception);
            }

            logger.LogInfo(
                $"Detached runtime from process {processId}.");
            PublishRoster();
            return true;
        }

        public async ValueTask DisposeAsync()
        {
            var isFirstDispose =
                Interlocked.Exchange(ref disposeState, 1) == 0;
            if (!isFirstDispose)
                return;

            var processIds = clients.Keys.ToArray();
            foreach (var processId in processIds)
                await DetachAsync(processId).ConfigureAwait(false);
        }

        private void ThrowIfDisposing()
        {
            ObjectDisposedException.ThrowIf(
                Volatile.Read(ref disposeState) != 0,
                this);
        }

        private bool TryFindRuntime(
            int processId,
            out ClientRuntimeViewModel viewModel)
            => clients.TryGetValue(processId, out viewModel);

        private void OnRuntimePropertyChanged(
            object sender,
            PropertyChangedEventArgs e)
        {
            if (string.Equals(
                    e.PropertyName,
                    nameof(ClientRuntimeViewModel.Current),
                    StringComparison.Ordinal) ||
                string.Equals(
                    e.PropertyName,
                    nameof(ClientRuntimeViewModel.LatestCapture),
                    StringComparison.Ordinal))
            {
                PublishRoster();
            }
        }

        private void PublishRoster()
        {
            var entries = clients.Values
                .Select(CreateRosterEntry)
                .Where(entry => entry is not null)
                .OrderBy(
                    entry => entry.Client.InstanceId,
                    StringComparer.Ordinal)
                .GroupBy(
                    entry => entry.CharacterName,
                    StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToImmutableArray();

            ClientRosterSnapshot snapshot;
            lock (rosterGate)
            {
                if (lastRoster.SequenceEqual(entries))
                    return;

                lastRoster = entries;
                snapshot = new ClientRosterSnapshot(
                    new ClientRosterSequence(
                        checked(++rosterSequence)),
                    clock.GetCurrentTimestamp(),
                    entries);
            }

            foreach (var runtime in clients.Values.ToArray())
            {
                try
                {
                    runtime.PublishClientRoster(snapshot);
                }
                catch (ObjectDisposedException)
                {
                }
            }
        }

        private static ClientRosterEntry CreateRosterEntry(
            ClientRuntimeViewModel runtime)
        {
            var snapshot = runtime.LatestSnapshot;
            var characterName = snapshot?.Character?.Name;
            if (snapshot is null ||
                string.IsNullOrWhiteSpace(characterName))
            {
                return null;
            }

            var view = runtime.Current;
            var isWaitingForMana =
                view?.SpellCast?.Status ==
                    SpellCastStatus.WaitingForMana ||
                view?.SkillUse?.Status ==
                    SkillUseStatus.WaitingForMana ||
                view?.Flower?.Status ==
                    FlowerStatus.WaitingForMana;

            return new ClientRosterEntry(
                runtime.Client,
                characterName,
                snapshot.Presence,
                view?.Lifecycle == MacroLifecycle.Running,
                isWaitingForMana,
                snapshot.Location,
                snapshot.Vitals,
                view?.Flower?.FloweredAt);
        }

    }
}
