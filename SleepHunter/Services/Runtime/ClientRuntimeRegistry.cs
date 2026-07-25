using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SleepHunter.Interop.Hosting;
using SleepHunter.Interop.Snapshots;
using SleepHunter.Services.Logging;
using SleepHunter.ViewModels;

namespace SleepHunter.Services.Runtime
{
    public sealed class ClientRuntimeRegistry : IAsyncDisposable
    {
        private readonly ConcurrentDictionary<
            int,
            ClientRuntimeViewModel> clients = new();
        private readonly IClientRuntimeFactory factory;
        private readonly ILogger logger;
        private readonly string mappingPath;
        private readonly TimeProvider timeProvider;
        private readonly IUiDispatcher uiDispatcher;

        private int disposeState;

        public ClientRuntimeRegistry(
            IClientRuntimeFactory factory,
            ILogger logger,
            IUiDispatcher uiDispatcher,
            string mappingPath,
            TimeProvider timeProvider)
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

            if (!string.Equals(
                    descriptor.Client.Version,
                    Usda741SnapshotCapture.SupportedVersion,
                    StringComparison.OrdinalIgnoreCase))
            {
                logger.LogInfo(
                    $"Skipping shadow runtime for unsupported client version '{descriptor.Client.Version}' (pid {descriptor.ProcessId}).");
                return false;
            }

            IClientRuntimeHost host;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var mappingStream = File.OpenRead(mappingPath);
                host = new ReadOnlyClientRuntimeHost(
                    factory.Attach(
                        mappingStream,
                        descriptor.Client,
                        descriptor.ProcessId,
                        descriptor.WindowHandle,
                        new SnapshotCaptureSchedule(
                            snapshotInterval,
                            SnapshotCaptureSections.All),
                        timeProvider));
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    $"Unable to attach shadow runtime to process {descriptor.ProcessId}.");
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

            if (Volatile.Read(ref disposeState) != 0)
            {
                if (clients.TryRemove(
                        descriptor.ProcessId,
                        out var attachedViewModel))
                {
                    await attachedViewModel
                        .DisposeAsync()
                        .ConfigureAwait(false);
                }

                ThrowIfDisposing();
            }

            logger.LogInfo(
                $"Attached read-only shadow runtime to process {descriptor.ProcessId}.");
            return true;
        }

        public bool TryFind(
            int processId,
            out ClientRuntimeViewModel viewModel) =>
            clients.TryGetValue(processId, out viewModel);

        public async ValueTask<bool> DetachAsync(int processId)
        {
            if (!clients.TryRemove(processId, out var viewModel))
                return false;

            try
            {
                await viewModel.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                logger.LogError(
                    $"Shadow runtime shutdown failed for process {processId}.");
                logger.LogException(exception);
            }

            logger.LogInfo(
                $"Detached shadow runtime from process {processId}.");
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
    }
}
