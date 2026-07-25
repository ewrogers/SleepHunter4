using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SleepHunter.Interop.Hosting;
using SleepHunter.Interop.Snapshots;
using SleepHunter.Runtime.Commands;
using SleepHunter.Runtime.Engine;
using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.ViewModels
{
    public sealed partial class ClientRuntimeViewModel :
        ObservableObject,
        IAsyncDisposable
    {
        private readonly CancellationTokenSource disposeCancellation = new();
        private readonly Task capturePump;
        private readonly IClientRuntimeHost host;
        private readonly IUiDispatcher uiDispatcher;
        private readonly Task viewPump;

        private MacroViewSnapshot current;
        private int disposeState;
        private volatile bool isHostAvailable = true;
        private SnapshotCaptureObservation latestCapture;

        public ClientRuntimeViewModel(
            IClientRuntimeHost host,
            IUiDispatcher uiDispatcher)
        {
            this.host = host ??
                throw new ArgumentNullException(nameof(host));
            this.uiDispatcher = uiDispatcher ??
                throw new ArgumentNullException(nameof(uiDispatcher));

            capturePump = PumpCapturesAsync(disposeCancellation.Token);
            viewPump = PumpViewsAsync(disposeCancellation.Token);
        }

        public ClientIdentity Client => host.Client;

        public MacroViewSnapshot Current
        {
            get => current;
            private set
            {
                if (SetProperty(ref current, value))
                    NotifyCommands();
            }
        }

        public SnapshotCaptureObservation LatestCapture
        {
            get => latestCapture;
            private set
            {
                if (!SetProperty(ref latestCapture, value))
                    return;

                OnPropertyChanged(nameof(CaptureError));
                OnPropertyChanged(nameof(CaptureSequence));
                OnPropertyChanged(nameof(CaptureStatistics));
                OnPropertyChanged(nameof(HasCapture));
                OnPropertyChanged(nameof(IsCaptureHealthy));
                OnPropertyChanged(nameof(LatestCaptureResult));
                OnPropertyChanged(nameof(LatestSnapshot));
            }
        }

        public SnapshotCaptureError CaptureError =>
            LatestCapture?.Result.Error;

        public SnapshotSequence? CaptureSequence =>
            LatestCapture?.Result.Metrics.Sequence;

        public SnapshotCaptureStatistics CaptureStatistics =>
            LatestCapture?.Statistics ??
            host.CaptureStatistics;

        public bool HasCapture => LatestCapture is not null;

        public bool IsCaptureHealthy =>
            LatestCapture?.Result.Succeeded == true;

        public SnapshotCaptureResult LatestCaptureResult =>
            LatestCapture?.Result;

        public ClientSnapshot LatestSnapshot =>
            LatestCapture?.Result.Snapshot;

        public ValueTask SendCommandAsync(
            MacroCommand command,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposing();
            return host.SendCommandAsync(command, cancellationToken);
        }

        public async ValueTask DisposeAsync()
        {
            var isFirstDispose =
                Interlocked.Exchange(ref disposeState, 1) == 0;
            if (!isFirstDispose)
            {
                await Task
                    .WhenAll(capturePump, viewPump)
                    .ConfigureAwait(false);
                return;
            }

            StartCommand.Cancel();
            PauseCommand.Cancel();
            ResumeCommand.Cancel();
            StopCommand.Cancel();
            disposeCancellation.Cancel();
            NotifyCommands();

            try
            {
                await Task
                    .WhenAll(
                        host.DisposeAsync().AsTask(),
                        capturePump,
                        viewPump)
                    .ConfigureAwait(false);
            }
            finally
            {
                disposeCancellation.Dispose();
            }
        }

        [RelayCommand(CanExecute = nameof(CanStart))]
        private Task StartAsync(CancellationToken cancellationToken) =>
            SendAsync(new StartMacroCommand(), cancellationToken);

        [RelayCommand(CanExecute = nameof(CanPause))]
        private Task PauseAsync(CancellationToken cancellationToken) =>
            SendAsync(new PauseMacroCommand(), cancellationToken);

        [RelayCommand(CanExecute = nameof(CanResume))]
        private Task ResumeAsync(CancellationToken cancellationToken) =>
            SendAsync(new ResumeMacroCommand(), cancellationToken);

        [RelayCommand(CanExecute = nameof(CanStop))]
        private Task StopAsync(CancellationToken cancellationToken) =>
            SendAsync(new StopMacroCommand(), cancellationToken);

        private async Task PumpCapturesAsync(
            CancellationToken cancellationToken)
        {
            try
            {
                await foreach (var capture in host.Captures
                                   .ReadAllAsync(cancellationToken)
                                   .ConfigureAwait(false))
                {
                    await uiDispatcher
                        .InvokeAsync(
                            () => LatestCapture = capture,
                            cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
            }
        }

        private async Task PumpViewsAsync(
            CancellationToken cancellationToken)
        {
            try
            {
                await foreach (var view in host.Views
                                   .ReadAllAsync(cancellationToken)
                                   .ConfigureAwait(false))
                {
                    await uiDispatcher
                        .InvokeAsync(
                            () => Current = view,
                            cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
            }
            finally
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    await uiDispatcher
                        .InvokeAsync(
                            () =>
                            {
                                isHostAvailable = false;
                                NotifyCommands();
                            },
                            CancellationToken.None)
                        .ConfigureAwait(false);
                }
            }
        }

        private bool CanChangeLifecycle(MacroLifecycle lifecycle) =>
            Volatile.Read(ref disposeState) == 0 &&
            isHostAvailable &&
            Current?.Lifecycle == lifecycle;

        private bool CanStart() =>
            CanChangeLifecycle(MacroLifecycle.Stopped);

        private bool CanPause() =>
            CanChangeLifecycle(MacroLifecycle.Running);

        private bool CanResume() =>
            CanChangeLifecycle(MacroLifecycle.Paused);

        private bool CanStop() =>
            Volatile.Read(ref disposeState) == 0 &&
            isHostAvailable &&
            Current?.Lifecycle is
                MacroLifecycle.Running or MacroLifecycle.Paused;

        private void NotifyCommands()
        {
            StartCommand.NotifyCanExecuteChanged();
            PauseCommand.NotifyCanExecuteChanged();
            ResumeCommand.NotifyCanExecuteChanged();
            StopCommand.NotifyCanExecuteChanged();
        }

        private Task SendAsync(
            MacroCommand command,
            CancellationToken cancellationToken)
        {
            ThrowIfDisposing();
            return host
                .SendCommandAsync(command, cancellationToken)
                .AsTask();
        }

        private void ThrowIfDisposing()
        {
            ObjectDisposedException.ThrowIf(
                Volatile.Read(ref disposeState) != 0,
                this);
        }
    }
}
