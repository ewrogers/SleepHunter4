using System.Threading.Channels;
using SleepHunter.Interop.Input;
using SleepHunter.Interop.Snapshots;
using SleepHunter.Runtime.Actions;
using SleepHunter.Runtime.Commands;
using SleepHunter.Runtime.Engine;
using SleepHunter.Runtime.Hosting;
using SleepHunter.Runtime.Intents;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Interop.Hosting;

public sealed class ClientRuntimeHost : IClientRuntimeHost
{
    private readonly CancellationTokenSource disposeCancellation = new();
    private readonly ClientIntentExecutor intentExecutor;
    private readonly MacroSession session;
    private readonly ClientSnapshotScheduler snapshotScheduler;
    private readonly IClientWindowTargetProvider targetProvider;
    private readonly Task worker;

    private int disposeState;
    private SnapshotCaptureResult? latestCaptureResult;
    private ClientIntentIssueResult? lastIntentIssueResult;
    private ClientSnapshot? latestSnapshot;

    public ClientRuntimeHost(
        IClientSnapshotCapture snapshotCapture,
        SnapshotCaptureSchedule snapshotSchedule,
        ClientIntentExecutor intentExecutor,
        IClientWindowTargetProvider targetProvider,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(snapshotCapture);
        ArgumentNullException.ThrowIfNull(snapshotSchedule);
        ArgumentNullException.ThrowIfNull(intentExecutor);
        ArgumentNullException.ThrowIfNull(targetProvider);
        ArgumentNullException.ThrowIfNull(timeProvider);

        if (snapshotCapture.Client != targetProvider.Client)
        {
            throw new ArgumentException(
                "Snapshot capture and window input must target the same client.",
                nameof(targetProvider));
        }

        Client = snapshotCapture.Client;
        this.intentExecutor = intentExecutor;
        this.targetProvider = targetProvider;
        session = new MacroSession(
            new MacroEngine(),
            new MacroClock(timeProvider));
        snapshotScheduler = new ClientSnapshotScheduler(
            snapshotCapture,
            snapshotSchedule,
            timeProvider);
        worker = RunAsync();
    }

    public ClientIdentity Client { get; }

    public ChannelReader<MacroViewSnapshot> Views => session.Views;

    public SnapshotCaptureResult? LatestCaptureResult =>
        Volatile.Read(ref latestCaptureResult);

    public ClientIntentIssueResult? LastIntentIssueResult =>
        Volatile.Read(ref lastIntentIssueResult);

    public SnapshotCaptureStatistics CaptureStatistics =>
        snapshotScheduler.Statistics;

    public Task Completion => worker;

    public ValueTask SendCommandAsync(
        MacroCommand command,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposing();
        return session.SendCommandAsync(command, cancellationToken);
    }

    public bool PublishClientRoster(ClientRosterSnapshot snapshot)
    {
        ThrowIfDisposing();
        return session.PublishClientRoster(snapshot);
    }

    public async ValueTask DisposeAsync()
    {
        var isFirstDispose = Interlocked.Exchange(ref disposeState, 1) == 0;
        if (isFirstDispose)
        {
            disposeCancellation.Cancel();
        }

        try
        {
            await worker.ConfigureAwait(false);
        }
        finally
        {
            if (isFirstDispose)
            {
                disposeCancellation.Dispose();
            }
        }
    }

    private async Task RunAsync()
    {
        try
        {
            await Task
                .WhenAll(
                    PumpSnapshotsAsync(disposeCancellation.Token),
                    PumpIntentsAsync(disposeCancellation.Token))
                .ConfigureAwait(false);
        }
        finally
        {
            disposeCancellation.Cancel();
            try
            {
                await snapshotScheduler.DisposeAsync().ConfigureAwait(false);
            }
            finally
            {
                await session.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    private async Task PumpSnapshotsAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var result in snapshotScheduler.Results
                               .ReadAllAsync(cancellationToken)
                               .ConfigureAwait(false))
            {
                Volatile.Write(ref latestSnapshot, result.Snapshot);
                Volatile.Write(ref latestCaptureResult, result);
                if (result.Snapshot is not { } snapshot)
                {
                    continue;
                }

                session.PublishSnapshot(snapshot);
            }
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
        }
        catch
        {
            disposeCancellation.Cancel();
            throw;
        }
    }

    private async Task PumpIntentsAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var intent in session.Intents
                               .ReadAllAsync(cancellationToken)
                               .ConfigureAwait(false))
            {
                if (intent is not ClientActionIntent clientAction)
                {
                    throw new InvalidOperationException(
                        $"The client runtime host cannot execute intent type '{intent.GetType().Name}'.");
                }

                var issue = Execute(clientAction);
                await session
                    .ReportActionIssueAsync(issue, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
        }
        catch
        {
            disposeCancellation.Cancel();
            throw;
        }
    }

    private ClientActionIssue Execute(ClientActionIntent intent)
    {
        var snapshot = Volatile.Read(ref latestSnapshot);
        if (snapshot is null ||
            !targetProvider.TryGetTarget(out var target) ||
            target is null)
        {
            return new ClientActionIssue(
                intent.ActionId,
                ClientActionIssueStatus.Rejected);
        }

        if (target.Client != Client)
        {
            throw new InvalidOperationException(
                "The window target provider returned a different client.");
        }

        var result = intentExecutor.Execute(intent, target, snapshot);
        Volatile.Write(ref lastIntentIssueResult, result);
        return result.ToActionIssue();
    }

    private void ThrowIfDisposing()
    {
        ObjectDisposedException.ThrowIf(
            Volatile.Read(ref disposeState) != 0,
            this);
    }
}
