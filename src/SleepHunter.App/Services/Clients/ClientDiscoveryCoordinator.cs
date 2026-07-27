using System;
using System.Threading;
using System.Threading.Tasks;
using SleepHunter.Services.Logging;
using SleepHunter.ViewModels;

namespace SleepHunter.Services.Clients
{
    public sealed class ClientDiscoveryCoordinator :
        IDisposable,
        IAsyncDisposable
    {
        private static readonly TimeSpan MaximumInterval =
            TimeSpan.FromDays(1);
        private static readonly TimeSpan MinimumInterval =
            TimeSpan.FromMilliseconds(10);

        private readonly CancellationTokenSource cancellation = new();
        private readonly Func<TimeSpan> getProcessInterval;
        private readonly object lifecycleGate = new();
        private readonly ILogger logger;
        private readonly Action reconcileProcesses;
        private readonly Action scanProcesses;
        private readonly TimeProvider timeProvider;
        private readonly IUiDispatcher uiDispatcher;

        private Task completion = Task.CompletedTask;
        private bool isDisposed;
        private bool isStarted;
        private bool isStopping;

        public ClientDiscoveryCoordinator(
            Action scanProcesses,
            Action reconcileProcesses,
            Func<TimeSpan> getProcessInterval,
            IUiDispatcher uiDispatcher,
            TimeProvider timeProvider,
            ILogger logger)
        {
            this.scanProcesses = scanProcesses ??
                throw new ArgumentNullException(
                    nameof(scanProcesses));
            this.reconcileProcesses = reconcileProcesses ??
                throw new ArgumentNullException(
                    nameof(reconcileProcesses));
            this.getProcessInterval = getProcessInterval ??
                throw new ArgumentNullException(
                    nameof(getProcessInterval));
            this.uiDispatcher = uiDispatcher ??
                throw new ArgumentNullException(
                    nameof(uiDispatcher));
            this.timeProvider = timeProvider ??
                throw new ArgumentNullException(
                    nameof(timeProvider));
            this.logger = logger ??
                throw new ArgumentNullException(nameof(logger));
        }

        public bool IsRunning
        {
            get
            {
                lock (lifecycleGate)
                {
                    return isStarted &&
                           !isStopping &&
                           !completion.IsCompleted;
                }
            }
        }

        public Task Completion
        {
            get
            {
                lock (lifecycleGate)
                {
                    return completion;
                }
            }
        }

        public void Start()
        {
            lock (lifecycleGate)
            {
                ObjectDisposedException.ThrowIf(
                    isDisposed || isStopping,
                    this);
                if (isStarted)
                    return;

                isStarted = true;
                var processLoop = Task.Run(
                    () => RunLoopAsync(
                        PollProcessesAsync,
                        getProcessInterval,
                        "process scanner",
                        cancellation.Token));
                completion = processLoop;
            }

            logger.LogInfo(
                "Client polling coordinator has started");
        }

        public void Dispose()
        {
            lock (lifecycleGate)
            {
                if (isStopping || isDisposed)
                    return;

                isStopping = true;
                cancellation.Cancel();
            }
        }

        public async ValueTask DisposeAsync()
        {
            Dispose();

            Task pending;
            lock (lifecycleGate)
            {
                pending = completion;
            }

            try
            {
                await pending.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
                when (cancellation.IsCancellationRequested)
            {
            }

            lock (lifecycleGate)
            {
                if (isDisposed)
                    return;

                cancellation.Dispose();
                isDisposed = true;
            }
        }

        private async Task RunLoopAsync(
            Func<CancellationToken, ValueTask> poll,
            Func<TimeSpan> getInterval,
            string operationName,
            CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await poll(cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    logger.LogException(exception);
                    logger.LogError(
                        $"The {operationName} iteration failed");
                }

                try
                {
                    await Task.Delay(
                            NormalizeInterval(getInterval()),
                            timeProvider,
                            cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    logger.LogException(exception);
                    logger.LogError(
                        $"Unable to schedule the next {operationName} iteration");
                    await Task.Delay(
                            MinimumInterval,
                            timeProvider,
                            cancellationToken)
                        .ConfigureAwait(false);
                }
            }
        }

        private async ValueTask PollProcessesAsync(
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            scanProcesses();
            await uiDispatcher.InvokeAsync(
                reconcileProcesses,
                cancellationToken);
        }

        private static TimeSpan NormalizeInterval(
            TimeSpan interval)
        {
            if (interval < MinimumInterval)
                return MinimumInterval;

            return interval > MaximumInterval
                ? MaximumInterval
                : interval;
        }
    }
}
