using System.Threading.Channels;
using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Interop.Snapshots;

public sealed class ClientSnapshotScheduler : IAsyncDisposable
{
    private readonly IClientSnapshotCapture capture;
    private readonly CancellationTokenSource disposeCancellation = new();
    private readonly Channel<SnapshotCaptureObservation> results;
    private readonly SnapshotCaptureSchedule schedule;
    private readonly SnapshotTimingAggregator timing;
    private readonly TimeProvider timeProvider;
    private readonly Task worker;
    private readonly SnapshotSequence firstSequence;

    private int disposeState;
    private SnapshotCaptureStatistics statistics;

    public ClientSnapshotScheduler(
        IClientSnapshotCapture capture,
        SnapshotCaptureSchedule schedule,
        TimeProvider timeProvider)
        : this(
            capture,
            schedule,
            timeProvider,
            new SnapshotSequence(1))
    {
    }

    public ClientSnapshotScheduler(
        IClientSnapshotCapture capture,
        SnapshotCaptureSchedule schedule,
        TimeProvider timeProvider,
        SnapshotSequence firstSequence)
    {
        ArgumentNullException.ThrowIfNull(capture);
        ArgumentNullException.ThrowIfNull(schedule);
        ArgumentNullException.ThrowIfNull(timeProvider);

        if (firstSequence.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(firstSequence),
                firstSequence,
                "The first snapshot sequence must be positive.");
        }

        this.capture = capture;
        this.schedule = schedule;
        this.timeProvider = timeProvider;
        this.firstSequence = firstSequence;
        timing = new SnapshotTimingAggregator(schedule.TimingWindowCapacity);
        statistics = SnapshotCaptureStatistics.Empty(
            schedule.TimingWindowCapacity);
        results = Channel.CreateBounded<SnapshotCaptureObservation>(
            new BoundedChannelOptions(1)
            {
                AllowSynchronousContinuations = false,
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = true
            });
        worker = Task.Run(
            () => RunAsync(disposeCancellation.Token),
            CancellationToken.None);
    }

    public ClientIdentity Client => capture.Client;

    public SnapshotCaptureSchedule Schedule => schedule;

    public SnapshotSequence FirstSequence => firstSequence;

    public ChannelReader<SnapshotCaptureObservation> Results =>
        results.Reader;

    public SnapshotCaptureStatistics Statistics =>
        Volatile.Read(ref statistics);

    public Task Completion => worker;

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

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        Exception? completionError = null;

        try
        {
            using var timer = new PeriodicTimer(
                schedule.Interval,
                timeProvider);
            if (!schedule.CaptureImmediately &&
                !await timer
                    .WaitForNextTickAsync(cancellationToken)
                    .ConfigureAwait(false))
            {
                return;
            }

            var nextSequence = firstSequence.Value;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Capture(new SnapshotSequence(nextSequence));
                nextSequence = checked(nextSequence + 1);

                if (!await timer
                    .WaitForNextTickAsync(cancellationToken)
                    .ConfigureAwait(false))
                {
                    return;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            completionError = exception;
            throw;
        }
        finally
        {
            results.Writer.TryComplete(completionError);
        }
    }

    private void Capture(SnapshotSequence sequence)
    {
        var result = capture.Capture(sequence, schedule.Sections);
        if (result.Metrics.Sequence != sequence)
        {
            throw new InvalidOperationException(
                "The snapshot capture result sequence does not match the requested sequence.");
        }

        if (result.Snapshot is not null &&
            result.Snapshot.Client != capture.Client)
        {
            throw new InvalidOperationException(
                "The captured snapshot belongs to a different client.");
        }

        timing.Record(result);
        var updatedStatistics = timing.CreateStatistics();
        Volatile.Write(ref statistics, updatedStatistics);
        if (!results.Writer.TryWrite(
                new SnapshotCaptureObservation(
                    result,
                    updatedStatistics)))
        {
            throw new InvalidOperationException(
                "The snapshot result channel is unavailable.");
        }
    }
}
