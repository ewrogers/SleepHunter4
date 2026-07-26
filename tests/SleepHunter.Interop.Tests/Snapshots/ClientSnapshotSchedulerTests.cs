using System.Collections.Concurrent;
using System.Collections.Immutable;
using SleepHunter.Interop.Memory;
using SleepHunter.Interop.Snapshots;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Interop.Tests.Snapshots;

public sealed class ClientSnapshotSchedulerTests
{
    private static readonly ClientIdentity Client = new("process:1234");

    [Test]
    public async Task ShouldCaptureImmediatelyAndAtTheConfiguredCadence()
    {
        var timeProvider = new ManualTimeProvider();
        var capture = new ScriptedCapture(
            sequence => CreateSuccess(sequence, sequence.Value));
        await using var scheduler = new ClientSnapshotScheduler(
            capture,
            new SnapshotCaptureSchedule(
                TimeSpan.FromMilliseconds(100),
                SnapshotCaptureSections.Inventory),
            timeProvider,
            new SnapshotSequence(41));

        var first = await ReadObservationAsync(scheduler.Results);
        timeProvider.Advance(TimeSpan.FromMilliseconds(100));
        var second = await ReadObservationAsync(scheduler.Results);
        await WaitForSampleCountAsync(scheduler, 2);

        Assert.Multiple(() =>
        {
            Assert.That(
                first.Result.Metrics.Sequence.Value,
                Is.EqualTo(41));
            Assert.That(
                second.Result.Metrics.Sequence.Value,
                Is.EqualTo(42));
            Assert.That(first.Statistics.SampleCount, Is.EqualTo(1));
            Assert.That(second.Statistics.SampleCount, Is.EqualTo(2));
            Assert.That(
                capture.Requests,
                Is.EqualTo(
                    new[]
                    {
                        (
                            new SnapshotSequence(41),
                            SnapshotCaptureSections.Inventory),
                        (
                            new SnapshotSequence(42),
                            SnapshotCaptureSections.Inventory)
                    }));
            Assert.That(scheduler.FirstSequence.Value, Is.EqualTo(41));
            Assert.That(scheduler.Statistics.SampleCount, Is.EqualTo(2));
            Assert.That(scheduler.Statistics.SucceededCount, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task ShouldCoalesceUnreadResultsToTheNewestCapture()
    {
        var timeProvider = new ManualTimeProvider();
        var capture = new ScriptedCapture(
            sequence => CreateSuccess(sequence, sequence.Value));
        await using var scheduler = new ClientSnapshotScheduler(
            capture,
            new SnapshotCaptureSchedule(TimeSpan.FromMilliseconds(100)),
            timeProvider);

        await WaitForSampleCountAsync(scheduler, 1);
        timeProvider.Advance(TimeSpan.FromMilliseconds(100));
        await WaitForSampleCountAsync(scheduler, 2);
        timeProvider.Advance(TimeSpan.FromMilliseconds(100));
        await WaitForSampleCountAsync(scheduler, 3);

        var didRead = scheduler.Results.TryRead(out var newest);
        var didReadAgain = scheduler.Results.TryRead(out _);

        Assert.Multiple(() =>
        {
            Assert.That(didRead, Is.True);
            Assert.That(
                newest!.Result.Metrics.Sequence.Value,
                Is.EqualTo(3));
            Assert.That(didReadAgain, Is.False);
            Assert.That(scheduler.Statistics.SampleCount, Is.EqualTo(3));
        });
    }

    [Test]
    public async Task ShouldCoalesceMissedPeriodsWithoutOverlappingCaptures()
    {
        var timeProvider = new ManualTimeProvider();
        using var firstCaptureStarted = new ManualResetEventSlim();
        using var releaseFirstCapture = new ManualResetEventSlim();
        var capture = new ScriptedCapture(
            sequence =>
            {
                if (sequence.Value == 1)
                {
                    firstCaptureStarted.Set();
                    if (!releaseFirstCapture.Wait(TimeSpan.FromSeconds(5)))
                    {
                        throw new TimeoutException(
                            "The blocked capture was not released.");
                    }
                }

                return CreateSuccess(sequence, sequence.Value);
            });
        await using var scheduler = new ClientSnapshotScheduler(
            capture,
            new SnapshotCaptureSchedule(TimeSpan.FromMilliseconds(100)),
            timeProvider);

        Assert.That(
            firstCaptureStarted.Wait(TimeSpan.FromSeconds(5)),
            Is.True);
        timeProvider.Advance(TimeSpan.FromMilliseconds(500));
        releaseFirstCapture.Set();
        await WaitForSampleCountAsync(scheduler, 2);

        Assert.Multiple(() =>
        {
            Assert.That(capture.CaptureCount, Is.EqualTo(2));
            Assert.That(capture.MaximumConcurrentCaptures, Is.EqualTo(1));
            Assert.That(scheduler.Statistics.SampleCount, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task ShouldPublishFailedCapturesAndContinueScheduling()
    {
        var timeProvider = new ManualTimeProvider();
        var capture = new ScriptedCapture(
            sequence => sequence.Value == 1
                ? CreateFailure(
                    sequence,
                    durationMilliseconds: 10,
                    SnapshotCaptureFailure.MappingReadFailed)
                : CreateSuccess(sequence, durationMilliseconds: 20));
        await using var scheduler = new ClientSnapshotScheduler(
            capture,
            new SnapshotCaptureSchedule(TimeSpan.FromMilliseconds(100)),
            timeProvider);

        var failed = await ReadObservationAsync(scheduler.Results);
        timeProvider.Advance(TimeSpan.FromMilliseconds(100));
        var succeeded = await ReadObservationAsync(scheduler.Results);
        await WaitForSampleCountAsync(scheduler, 2);

        Assert.Multiple(() =>
        {
            Assert.That(failed.Result.Succeeded, Is.False);
            Assert.That(
                failed.Result.Error?.Failure,
                Is.EqualTo(SnapshotCaptureFailure.MappingReadFailed));
            Assert.That(failed.Statistics.FailedCount, Is.EqualTo(1));
            Assert.That(succeeded.Result.Succeeded, Is.True);
            Assert.That(succeeded.Statistics.SampleCount, Is.EqualTo(2));
            Assert.That(scheduler.Statistics.SucceededCount, Is.EqualTo(1));
            Assert.That(scheduler.Statistics.FailedCount, Is.EqualTo(1));
            Assert.That(
                scheduler.Statistics.Failures[
                    SnapshotCaptureFailure.MappingReadFailed],
                Is.EqualTo(1));
        });
    }

    [Test]
    public void ShouldAggregateBoundedCaptureAndSectionTiming()
    {
        var aggregator = new SnapshotTimingAggregator(capacity: 4);
        aggregator.Record(CreateSuccess(new SnapshotSequence(1), 10));
        aggregator.Record(
            CreateFailure(
                new SnapshotSequence(2),
                durationMilliseconds: 20,
                SnapshotCaptureFailure.MappingReadFailed));
        aggregator.Record(
            CreateFailure(
                new SnapshotSequence(3),
                durationMilliseconds: 30,
                SnapshotCaptureFailure.InvalidValue));
        aggregator.Record(CreateSuccess(new SnapshotSequence(4), 40));
        aggregator.Record(CreateSuccess(new SnapshotSequence(5), 100));

        var statistics = aggregator.CreateStatistics();
        var presence = statistics.Sections.Single(
            section => section.Section == SnapshotSection.Presence);

        Assert.Multiple(() =>
        {
            Assert.That(statistics.WindowCapacity, Is.EqualTo(4));
            Assert.That(statistics.SampleCount, Is.EqualTo(4));
            Assert.That(statistics.SucceededCount, Is.EqualTo(2));
            Assert.That(statistics.FailedCount, Is.EqualTo(2));
            Assert.That(
                statistics.Duration.Minimum,
                Is.EqualTo(TimeSpan.FromMilliseconds(20)));
            Assert.That(
                statistics.Duration.Average,
                Is.EqualTo(TimeSpan.FromMilliseconds(47.5)));
            Assert.That(
                statistics.Duration.Median,
                Is.EqualTo(TimeSpan.FromMilliseconds(35)));
            Assert.That(
                statistics.Duration.Percentile95,
                Is.EqualTo(TimeSpan.FromMilliseconds(100)));
            Assert.That(
                statistics.Duration.Maximum,
                Is.EqualTo(TimeSpan.FromMilliseconds(100)));
            Assert.That(
                statistics.Failures[SnapshotCaptureFailure.MappingReadFailed],
                Is.EqualTo(1));
            Assert.That(
                statistics.Failures[SnapshotCaptureFailure.InvalidValue],
                Is.EqualTo(1));
            Assert.That(statistics.Reads.RequestCount, Is.EqualTo(4));
            Assert.That(statistics.Reads.FailedReadCount, Is.EqualTo(2));
            Assert.That(presence.SucceededCount, Is.EqualTo(2));
            Assert.That(presence.FailedCount, Is.EqualTo(2));
        });
    }

    [Test]
    public void ShouldValidateCaptureSchedule()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = new SnapshotCaptureSchedule(TimeSpan.Zero));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = new SnapshotCaptureSchedule(
                    TimeSpan.FromTicks(1)));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = new SnapshotCaptureSchedule(
                    SnapshotCaptureSchedule.MaximumInterval +
                    TimeSpan.FromMilliseconds(1)));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = new SnapshotCaptureSchedule(
                    TimeSpan.FromMilliseconds(1),
                    (SnapshotCaptureSections)(1 << 10)));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = new SnapshotCaptureSchedule(
                    TimeSpan.FromMilliseconds(1),
                    timingWindowCapacity: 0));
        });
    }

    [Test]
    public void ShouldRejectAnInvalidFirstSequence()
    {
        var capture = new ScriptedCapture(
            sequence => CreateSuccess(sequence, sequence.Value));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => _ = new ClientSnapshotScheduler(
                capture,
                new SnapshotCaptureSchedule(TimeSpan.FromMilliseconds(100)),
                TimeProvider.System,
                default));
    }

    private static async Task<SnapshotCaptureObservation>
        ReadObservationAsync(
            System.Threading.Channels.ChannelReader<
                SnapshotCaptureObservation> reader)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try
        {
            return await reader.ReadAsync(timeout.Token);
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            throw new TimeoutException(
                "The expected snapshot result was not published.");
        }
    }

    private static async Task WaitForSampleCountAsync(
        ClientSnapshotScheduler scheduler,
        int expectedCount)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (scheduler.Statistics.SampleCount < expectedCount)
        {
            try
            {
                await Task.Delay(
                    TimeSpan.FromMilliseconds(1),
                    timeout.Token);
            }
            catch (OperationCanceledException) when (timeout.IsCancellationRequested)
            {
                throw new TimeoutException(
                    $"The scheduler sample count did not reach {expectedCount}.");
            }
        }
    }

    private static SnapshotCaptureResult CreateSuccess(
        SnapshotSequence sequence,
        long durationMilliseconds)
    {
        var startedAt = MacroTimestamp.Zero;
        var completedAt = new MacroTimestamp(
            TimeSpan.FromMilliseconds(durationMilliseconds));
        var reads = new MemoryReadMetrics(
            RequestCount: 1,
            TransportReadCount: 1,
            FailedReadCount: 0,
            RequestedBytes: 4,
            BytesRead: 4);
        var metrics = new SnapshotCaptureMetrics(
            sequence,
            startedAt,
            completedAt,
            ImmutableArray.Create(
                new SnapshotSectionMetrics(
                    SnapshotSection.Presence,
                    completedAt.Elapsed,
                    succeeded: true,
                    reads)),
            reads);
        var snapshot = new ClientSnapshot(
            sequence,
            startedAt,
            completedAt,
            Client,
            SnapshotQuality.Complete,
            ClientPresence.LoggedOut);
        return new SnapshotCaptureResult(
            snapshot,
            SnapshotQuality.Complete,
            error: null,
            metrics);
    }

    private static SnapshotCaptureResult CreateFailure(
        SnapshotSequence sequence,
        long durationMilliseconds,
        SnapshotCaptureFailure failure)
    {
        var startedAt = MacroTimestamp.Zero;
        var completedAt = new MacroTimestamp(
            TimeSpan.FromMilliseconds(durationMilliseconds));
        var reads = new MemoryReadMetrics(
            RequestCount: 1,
            TransportReadCount: 1,
            FailedReadCount: 1,
            RequestedBytes: 4,
            BytesRead: 0);
        var metrics = new SnapshotCaptureMetrics(
            sequence,
            startedAt,
            completedAt,
            ImmutableArray.Create(
                new SnapshotSectionMetrics(
                    SnapshotSection.Presence,
                    completedAt.Elapsed,
                    succeeded: false,
                    reads)),
            reads);
        var error = new SnapshotCaptureError(
            SnapshotSection.Presence,
            failure,
            "The scripted capture failed.");
        return new SnapshotCaptureResult(
            snapshot: null,
            SnapshotQuality.Partial,
            error,
            metrics);
    }

    private sealed class ScriptedCapture : IClientSnapshotCapture
    {
        private readonly Func<
            SnapshotSequence,
            SnapshotCaptureResult> capture;
        private readonly ConcurrentQueue<(
            SnapshotSequence Sequence,
            SnapshotCaptureSections Sections)> requests = new();

        private int captureCount;
        private int concurrentCaptures;
        private int maximumConcurrentCaptures;

        public ScriptedCapture(
            Func<SnapshotSequence, SnapshotCaptureResult> capture)
        {
            ArgumentNullException.ThrowIfNull(capture);
            this.capture = capture;
        }

        public ClientIdentity Client => ClientSnapshotSchedulerTests.Client;

        public int CaptureCount => Volatile.Read(ref captureCount);

        public int MaximumConcurrentCaptures =>
            Volatile.Read(ref maximumConcurrentCaptures);

        public (SnapshotSequence Sequence, SnapshotCaptureSections Sections)[]
            Requests => requests.ToArray();

        public SnapshotCaptureResult Capture(
            SnapshotSequence sequence,
            SnapshotCaptureSections sections = SnapshotCaptureSections.Core)
        {
            requests.Enqueue((sequence, sections));
            var concurrent = Interlocked.Increment(ref concurrentCaptures);
            UpdateMaximumConcurrentCaptures(concurrent);

            try
            {
                return capture(sequence);
            }
            finally
            {
                Interlocked.Decrement(ref concurrentCaptures);
                Interlocked.Increment(ref captureCount);
            }
        }

        private void UpdateMaximumConcurrentCaptures(int concurrent)
        {
            var observedMaximum = Volatile.Read(
                ref maximumConcurrentCaptures);
            while (concurrent > observedMaximum)
            {
                var previous = Interlocked.CompareExchange(
                    ref maximumConcurrentCaptures,
                    concurrent,
                    observedMaximum);
                if (previous == observedMaximum)
                {
                    return;
                }

                observedMaximum = previous;
            }
        }
    }
}
