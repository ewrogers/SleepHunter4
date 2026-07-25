using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Threading.Channels;
using SleepHunter.Interop.Hosting;
using SleepHunter.Interop.Input;
using SleepHunter.Interop.Memory;
using SleepHunter.Interop.Snapshots;
using SleepHunter.Interop.Tests.Snapshots;
using SleepHunter.Runtime.Actions;
using SleepHunter.Runtime.Automation.Panels;
using SleepHunter.Runtime.Commands;
using SleepHunter.Runtime.Engine;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Interop.Tests.Hosting;

public sealed class ClientRuntimeHostTests
{
    private static readonly ClientIdentity Client = new(
        "process:1234",
        Usda741ClientIntentPlanner.SupportedVersion);

    [Test]
    public async Task ShouldPublishSuccessfulCapturesIntoRuntimeViews()
    {
        var timeProvider = new ManualTimeProvider();
        var sink = new RecordingMessageSink();
        await using var host = CreateHost(
            timeProvider,
            new ScriptedCapture(CreateSuccess),
            new FixedTargetProvider(Client),
            sink);

        var view = await host.Views.ReadUntilAsync(
            current => current.LatestSnapshotSequence?.Value == 1);

        Assert.Multiple(() =>
        {
            Assert.That(view.Presence, Is.EqualTo(ClientPresence.InWorld));
            Assert.That(host.Client, Is.EqualTo(Client));
            Assert.That(
                host.LatestCaptureResult?.Metrics.Sequence.Value,
                Is.EqualTo(1));
            Assert.That(host.CaptureStatistics.SucceededCount, Is.EqualTo(1));
            Assert.That(sink.Attempts, Is.Empty);
        });
    }

    [Test]
    public async Task ShouldExecuteIntentsAndReportSuccessfulIssuance()
    {
        var timeProvider = new ManualTimeProvider();
        var sink = new RecordingMessageSink();
        await using var host = CreateHost(
            timeProvider,
            new ScriptedCapture(CreateSuccess),
            new FixedTargetProvider(Client),
            sink);
        await host.Views.ReadUntilAsync(
            current => current.LatestSnapshotSequence?.Value == 1);
        await host.SendCommandAsync(new StartMacroCommand());
        await host.Views.ReadUntilAsync(
            current => current.Lifecycle == MacroLifecycle.Running);

        await host.SendCommandAsync(
            new RequestPanelTransitionCommand(
                ClientPanel.TemuairSpells,
                new PanelTransitionPolicy(
                    TimeSpan.FromSeconds(1),
                    maximumAttempts: 1)));
        var issued = await host.Views.ReadUntilAsync(
            current =>
                current.LastActionIssue?.Status ==
                ClientActionIssueStatus.Issued);

        Assert.Multiple(() =>
        {
            Assert.That(issued.Lifecycle, Is.EqualTo(MacroLifecycle.Running));
            Assert.That(issued.PendingActionId, Is.Not.Null);
            Assert.That(
                host.LastIntentIssueResult?.Status,
                Is.EqualTo(ClientIntentIssueStatus.Issued));
            Assert.That(
                host.LastIntentIssueResult?.Dispatch?.PostedMessageCount,
                Is.EqualTo(sink.Attempts.Length));
            Assert.That(sink.Attempts, Is.Not.Empty);
        });
    }

    [Test]
    public async Task ShouldPauseWhenNativeInputFailsBeforeIssuance()
    {
        var timeProvider = new ManualTimeProvider();
        var sink = new RecordingMessageSink(failedAttemptIndex: 0);
        await using var host = CreateHost(
            timeProvider,
            new ScriptedCapture(CreateSuccess),
            new FixedTargetProvider(Client),
            sink);
        await StartAsync(host);

        await host.SendCommandAsync(
            new RequestPanelTransitionCommand(
                ClientPanel.TemuairSpells));
        var failed = await host.Views.ReadUntilAsync(
            current =>
                current.LastActionIssue?.Status ==
                ClientActionIssueStatus.Failed);

        Assert.Multiple(() =>
        {
            Assert.That(failed.Lifecycle, Is.EqualTo(MacroLifecycle.Paused));
            Assert.That(failed.PendingActionId, Is.Null);
            Assert.That(
                failed.PanelTransition?.Status,
                Is.EqualTo(PanelTransitionStatus.IssueFailed));
            Assert.That(
                host.LastIntentIssueResult?.Status,
                Is.EqualTo(ClientIntentIssueStatus.Failed));
            Assert.That(
                host.LastIntentIssueResult?.Dispatch?.PostedMessageCount,
                Is.Zero);
            Assert.That(
                host.LastIntentIssueResult?.Dispatch?.PostedCleanupMessageCount,
                Is.EqualTo(1));
            Assert.That(sink.Attempts.Length, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task ShouldRejectInputWhenTheWindowTargetIsUnavailable()
    {
        var timeProvider = new ManualTimeProvider();
        var sink = new RecordingMessageSink();
        var targetProvider = new FixedTargetProvider(
            Client,
            isAvailable: false);
        await using var host = CreateHost(
            timeProvider,
            new ScriptedCapture(CreateSuccess),
            targetProvider,
            sink);
        await StartAsync(host);

        await host.SendCommandAsync(
            new RequestPanelTransitionCommand(
                ClientPanel.TemuairSpells));
        var rejected = await host.Views.ReadUntilAsync(
            current =>
                current.LastActionIssue?.Status ==
                ClientActionIssueStatus.Rejected);

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Lifecycle, Is.EqualTo(MacroLifecycle.Paused));
            Assert.That(rejected.PendingActionId, Is.Null);
            Assert.That(host.LastIntentIssueResult, Is.Null);
            Assert.That(sink.Attempts, Is.Empty);
        });
    }

    [Test]
    public async Task ShouldIgnoreFailedCapturesAndContinueScheduling()
    {
        var timeProvider = new ManualTimeProvider();
        var capture = new ScriptedCapture(
            sequence => sequence.Value == 1
                ? CreateFailure(sequence)
                : CreateSuccess(sequence));
        await using var host = CreateHost(
            timeProvider,
            capture,
            new FixedTargetProvider(Client),
            new RecordingMessageSink());
        await WaitUntilAsync(
            () => host.LatestCaptureResult?.Metrics.Sequence.Value == 1);

        timeProvider.Advance(TimeSpan.FromMilliseconds(100));
        var recovered = await host.Views.ReadUntilAsync(
            current => current.LatestSnapshotSequence?.Value == 2);

        Assert.Multiple(() =>
        {
            Assert.That(recovered.Presence, Is.EqualTo(ClientPresence.InWorld));
            Assert.That(host.CaptureStatistics.SampleCount, Is.EqualTo(2));
            Assert.That(host.CaptureStatistics.SucceededCount, Is.EqualTo(1));
            Assert.That(host.CaptureStatistics.FailedCount, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task ShouldRejectInputAfterTheNewestCaptureFails()
    {
        var timeProvider = new ManualTimeProvider();
        var sink = new RecordingMessageSink();
        var capture = new ScriptedCapture(
            sequence => sequence.Value == 1
                ? CreateSuccess(sequence)
                : CreateFailure(sequence));
        await using var host = CreateHost(
            timeProvider,
            capture,
            new FixedTargetProvider(Client),
            sink);
        await StartAsync(host);
        timeProvider.Advance(TimeSpan.FromMilliseconds(100));
        await WaitUntilAsync(
            () => host.LatestCaptureResult?.Metrics.Sequence.Value == 2);

        await host.SendCommandAsync(
            new RequestPanelTransitionCommand(
                ClientPanel.TemuairSpells));
        var rejected = await host.Views.ReadUntilAsync(
            current =>
                current.LastActionIssue?.Status ==
                ClientActionIssueStatus.Rejected);

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Lifecycle, Is.EqualTo(MacroLifecycle.Paused));
            Assert.That(host.LatestCaptureResult?.Succeeded, Is.False);
            Assert.That(host.LastIntentIssueResult, Is.Null);
            Assert.That(sink.Attempts, Is.Empty);
        });
    }

    [Test]
    public async Task ShouldForwardClientRosterObservations()
    {
        await using var host = CreateHost(
            new ManualTimeProvider(),
            new ScriptedCapture(CreateSuccess),
            new FixedTargetProvider(Client),
            new RecordingMessageSink());
        var roster = new ClientRosterSnapshot(
            new ClientRosterSequence(7),
            MacroTimestamp.Zero,
            []);

        var wasPublished = host.PublishClientRoster(roster);
        var observed = await host.Views.ReadUntilAsync(
            current => current.ClientRosterSequence?.Value == 7);

        Assert.Multiple(() =>
        {
            Assert.That(wasPublished, Is.True);
            Assert.That(
                observed.ClientRosterSequence,
                Is.EqualTo(roster.Sequence));
        });
    }

    [Test]
    public async Task ShouldRejectOperationsAfterDisposal()
    {
        var host = CreateHost(
            new ManualTimeProvider(),
            new ScriptedCapture(CreateSuccess),
            new FixedTargetProvider(Client),
            new RecordingMessageSink());

        await host.DisposeAsync();

        Assert.Multiple(() =>
        {
            Assert.That(host.Completion.IsCompletedSuccessfully, Is.True);
            Assert.That(
                async () => await host.SendCommandAsync(
                    new StartMacroCommand()),
                Throws.TypeOf<ObjectDisposedException>());
            Assert.That(
                () => host.PublishClientRoster(
                    ClientRosterSnapshot.Empty),
                Throws.TypeOf<ObjectDisposedException>());
        });
    }

    [Test]
    public async Task ShouldTearDownTheSessionWhenAHostPumpFails()
    {
        var host = CreateHost(
            new ManualTimeProvider(),
            new ScriptedCapture(
                _ => throw new InvalidOperationException(
                    "The scripted capture failed unexpectedly.")),
            new FixedTargetProvider(Client),
            new RecordingMessageSink());

        Assert.That(
            async () => await host.Completion,
            Throws.TypeOf<InvalidOperationException>());
        while (host.Views.TryRead(out _))
        {
        }

        await host.Views.Completion.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.That(
            async () => await host.DisposeAsync(),
            Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void ShouldRequireSnapshotAndInputToTargetTheSameClient()
    {
        var capture = new ScriptedCapture(CreateSuccess);
        var otherClient = new ClientIdentity(
            "process:5678",
            Client.Version);

        Assert.That(
            () => _ = CreateHost(
                new ManualTimeProvider(),
                capture,
                new FixedTargetProvider(otherClient),
                new RecordingMessageSink()),
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void ShouldRejectAnUnavailableWindowsTarget()
    {
        var provider = new WindowsClientWindowTargetProvider(
            Client,
            processId: 1234,
            windowHandle: new nint(1));

        var isAvailable = provider.TryGetTarget(out var target);

        Assert.Multiple(() =>
        {
            Assert.That(isAvailable, Is.False);
            Assert.That(target, Is.Null);
        });
    }

    private static async Task StartAsync(ClientRuntimeHost host)
    {
        await host.Views.ReadUntilAsync(
            current => current.LatestSnapshotSequence?.Value == 1);
        await host.SendCommandAsync(new StartMacroCommand());
        await host.Views.ReadUntilAsync(
            current => current.Lifecycle == MacroLifecycle.Running);
    }

    private static ClientRuntimeHost CreateHost(
        TimeProvider timeProvider,
        IClientSnapshotCapture capture,
        IClientWindowTargetProvider targetProvider,
        RecordingMessageSink sink)
    {
        var executor = new ClientIntentExecutor(
            new Usda741ClientIntentPlanner(
                new FixedVirtualKeyMapper()),
            new WindowInputDispatcher(
                new ValidWindowGuard(),
                sink));
        return new ClientRuntimeHost(
            capture,
            new SnapshotCaptureSchedule(
                TimeSpan.FromMilliseconds(100)),
            executor,
            targetProvider,
            timeProvider);
    }

    private static SnapshotCaptureResult CreateSuccess(
        SnapshotSequence sequence)
    {
        var timestamp = new MacroTimestamp(
            TimeSpan.FromTicks(sequence.Value - 1));
        var reads = EmptyReads();
        var metrics = new SnapshotCaptureMetrics(
            sequence,
            timestamp,
            timestamp,
            ImmutableArray.Create(
                new SnapshotSectionMetrics(
                    SnapshotSection.Presence,
                    TimeSpan.Zero,
                    succeeded: true,
                    reads)),
            reads);
        var snapshot = new ClientSnapshot(
            sequence,
            timestamp,
            timestamp,
            Client,
            SnapshotQuality.Complete,
            ClientPresence.InWorld,
            ClientPanel.Inventory);
        return new SnapshotCaptureResult(
            snapshot,
            SnapshotQuality.Complete,
            error: null,
            metrics);
    }

    private static SnapshotCaptureResult CreateFailure(
        SnapshotSequence sequence)
    {
        var timestamp = new MacroTimestamp(
            TimeSpan.FromTicks(sequence.Value - 1));
        var reads = EmptyReads(failedReadCount: 1);
        var metrics = new SnapshotCaptureMetrics(
            sequence,
            timestamp,
            timestamp,
            ImmutableArray.Create(
                new SnapshotSectionMetrics(
                    SnapshotSection.Presence,
                    TimeSpan.Zero,
                    succeeded: false,
                    reads)),
            reads);
        return new SnapshotCaptureResult(
            snapshot: null,
            SnapshotQuality.Partial,
            new SnapshotCaptureError(
                SnapshotSection.Presence,
                SnapshotCaptureFailure.MappingReadFailed,
                "The scripted capture failed."),
            metrics);
    }

    private static MemoryReadMetrics EmptyReads(
        int failedReadCount = 0) =>
        new(
            RequestCount: failedReadCount,
            TransportReadCount: failedReadCount,
            FailedReadCount: failedReadCount,
            RequestedBytes: failedReadCount,
            BytesRead: 0);

    private static async Task WaitUntilAsync(Func<bool> predicate)
    {
        using var timeout = new CancellationTokenSource(
            TimeSpan.FromSeconds(5));
        try
        {
            while (!predicate())
            {
                await Task.Delay(
                    TimeSpan.FromMilliseconds(1),
                    timeout.Token);
            }
        }
        catch (OperationCanceledException)
            when (timeout.IsCancellationRequested)
        {
            throw new TimeoutException(
                "The expected host state was not observed.");
        }
    }

    private sealed class ScriptedCapture : IClientSnapshotCapture
    {
        private readonly Func<
            SnapshotSequence,
            SnapshotCaptureResult> capture;

        public ScriptedCapture(
            Func<SnapshotSequence, SnapshotCaptureResult> capture)
        {
            ArgumentNullException.ThrowIfNull(capture);
            this.capture = capture;
        }

        public ClientIdentity Client => ClientRuntimeHostTests.Client;

        public SnapshotCaptureResult Capture(
            SnapshotSequence sequence,
            SnapshotCaptureSections sections =
                SnapshotCaptureSections.Core) =>
            capture(sequence);
    }

    private sealed class FixedTargetProvider : IClientWindowTargetProvider
    {
        private readonly bool isAvailable;

        public FixedTargetProvider(
            ClientIdentity client,
            bool isAvailable = true)
        {
            Client = client;
            this.isAvailable = isAvailable;
        }

        public ClientIdentity Client { get; }

        public bool TryGetTarget(out ClientWindowTarget? target)
        {
            target = isAvailable
                ? new ClientWindowTarget(
                    Client,
                    processId: 1234,
                    windowHandle: new nint(0x1234),
                    clientWidth: 640,
                    clientHeight: 480)
                : null;
            return target is not null;
        }
    }

    private sealed class FixedVirtualKeyMapper : IVirtualKeyMapper
    {
        public bool TryMapScanCode(
            VirtualKey key,
            out byte scanCode)
        {
            scanCode = 1;
            return true;
        }
    }

    private sealed class ValidWindowGuard : IClientWindowGuard
    {
        public ClientWindowValidationResult Validate(
            ClientWindowTarget target) =>
            ClientWindowValidationResult.Valid;
    }

    private sealed class RecordingMessageSink : IWindowMessageSink
    {
        private readonly int? failedAttemptIndex;
        private readonly ConcurrentQueue<WindowInputMessage> attempts = new();

        public RecordingMessageSink(int? failedAttemptIndex = null)
        {
            this.failedAttemptIndex = failedAttemptIndex;
        }

        public WindowInputMessage[] Attempts =>
            attempts.ToArray();

        public bool TryPost(
            ClientWindowTarget target,
            WindowInputMessage message,
            out int nativeErrorCode)
        {
            var attemptIndex = attempts.Count;
            attempts.Enqueue(message);
            var succeeded = failedAttemptIndex != attemptIndex;
            nativeErrorCode = succeeded ? 0 : 5;
            return succeeded;
        }
    }
}

internal static class ChannelReaderExtensions
{
    public static async Task<T> ReadUntilAsync<T>(
        this ChannelReader<T> reader,
        Func<T, bool> predicate)
    {
        using var timeout = new CancellationTokenSource(
            TimeSpan.FromSeconds(5));
        try
        {
            await foreach (var value in reader.ReadAllAsync(timeout.Token))
            {
                if (predicate(value))
                {
                    return value;
                }
            }
        }
        catch (OperationCanceledException)
            when (timeout.IsCancellationRequested)
        {
            throw new TimeoutException(
                "The expected channel value was not published.");
        }

        throw new InvalidOperationException(
            "The channel completed before the expected value was published.");
    }
}
