using SleepHunter.Interop.Input;
using SleepHunter.Runtime.Actions;
using SleepHunter.Runtime.Automation.Panels;
using SleepHunter.Runtime.Intents;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Interop.Tests.Input;

public sealed class ClientIntentExecutorTests
{
    private static readonly ClientIdentity Client = new("process:1234");

    private static readonly ClientWindowTarget Target = new(
        Client,
        processId: 1234,
        windowHandle: new nint(0x1234),
        clientWidth: 640,
        clientHeight: 480);

    private static readonly ClientSnapshot Snapshot = new(
        new SnapshotSequence(1),
        MacroTimestamp.Zero,
        MacroTimestamp.Zero,
        Client,
        SnapshotQuality.Complete,
        ClientPresence.InWorld,
        ClientPanel.Inventory);

    private static readonly CancelDialogIntent Intent = new(
        new ClientActionId(1));

    [Test]
    public void ShouldIssueAPlannedIntent()
    {
        var sink = new RecordingMessageSink();
        var executor = CreateExecutor(
            ClientWindowValidationResult.Valid,
            sink);

        var result = executor.Execute(Intent, Target, Snapshot);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(ClientIntentIssueStatus.Issued));
            Assert.That(result.Plan.IsPlanned, Is.True);
            Assert.That(
                result.Dispatch?.Status,
                Is.EqualTo(WindowInputDispatchStatus.Issued));
            Assert.That(
                result.ToActionIssue().Status,
                Is.EqualTo(ClientActionIssueStatus.Issued));
            Assert.That(sink.Attempts, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void ShouldReturnAPlanningRejectionWithoutDispatch()
    {
        var sink = new RecordingMessageSink();
        var executor = CreateExecutor(
            ClientWindowValidationResult.Valid,
            sink);
        var snapshot = new ClientSnapshot(
            Snapshot.Sequence,
            Snapshot.CaptureStartedAt,
            Snapshot.CaptureCompletedAt,
            Snapshot.Client,
            SnapshotQuality.Partial,
            Snapshot.Presence,
            Snapshot.ActivePanel);

        var result = executor.Execute(Intent, Target, snapshot);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(ClientIntentIssueStatus.Rejected));
            Assert.That(
                result.Plan.Failure,
                Is.EqualTo(ClientIntentPlanFailure.SnapshotUnavailable));
            Assert.That(result.Dispatch, Is.Null);
            Assert.That(
                result.ToActionIssue().Status,
                Is.EqualTo(ClientActionIssueStatus.Rejected));
            Assert.That(sink.Attempts, Is.Empty);
        });
    }

    [Test]
    public void ShouldRetainWindowGuardDiagnosticsForAPlannedIntent()
    {
        var validation = new ClientWindowValidationResult(
            ClientWindowValidationFailure.ProcessMismatch,
            "The window owner changed.");
        var sink = new RecordingMessageSink();
        var executor = CreateExecutor(validation, sink);

        var result = executor.Execute(Intent, Target, Snapshot);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(ClientIntentIssueStatus.Rejected));
            Assert.That(result.Plan.IsPlanned, Is.True);
            Assert.That(
                result.Dispatch?.Status,
                Is.EqualTo(WindowInputDispatchStatus.Rejected));
            Assert.That(
                result.Dispatch?.Validation?.Failure,
                Is.EqualTo(ClientWindowValidationFailure.ProcessMismatch));
            Assert.That(
                result.ToActionIssue().Status,
                Is.EqualTo(ClientActionIssueStatus.Rejected));
            Assert.That(sink.Attempts, Is.Empty);
        });
    }

    [Test]
    public void ShouldReportPartialIssuanceAndCleanup()
    {
        var sink = new RecordingMessageSink(failedAttemptIndex: 1);
        var executor = CreateExecutor(
            ClientWindowValidationResult.Valid,
            sink);

        var result = executor.Execute(Intent, Target, Snapshot);

        Assert.Multiple(() =>
        {
            Assert.That(
                result.Status,
                Is.EqualTo(ClientIntentIssueStatus.PartiallyIssued));
            Assert.That(result.Dispatch?.PostedMessageCount, Is.EqualTo(1));
            Assert.That(
                result.Dispatch?.PostedCleanupMessageCount,
                Is.EqualTo(1));
            Assert.That(
                result.ToActionIssue().Status,
                Is.EqualTo(ClientActionIssueStatus.PartiallyIssued));
            Assert.That(sink.Attempts, Has.Count.EqualTo(3));
        });
    }

    [Test]
    public void ShouldMapFailedAndUnsupportedIssuance()
    {
        var failed = CreateExecutor(
            ClientWindowValidationResult.Valid,
            new RecordingMessageSink(failedAttemptIndex: 0))
            .Execute(Intent, Target, Snapshot);
        var unsupported = CreateExecutor(
            ClientWindowValidationResult.Valid,
            new RecordingMessageSink())
            .Execute(
                new UnsupportedIntent(new ClientActionId(2)),
                Target,
                Snapshot);

        Assert.Multiple(() =>
        {
            Assert.That(
                failed.ToActionIssue().Status,
                Is.EqualTo(ClientActionIssueStatus.Failed));
            Assert.That(
                unsupported.ToActionIssue().Status,
                Is.EqualTo(ClientActionIssueStatus.Unsupported));
        });
    }

    private static ClientIntentExecutor CreateExecutor(
        ClientWindowValidationResult validation,
        RecordingMessageSink sink)
    {
        var planner = new ClientIntentPlanner(
            new FixedVirtualKeyMapper());
        var dispatcher = new WindowInputDispatcher(
            new FixedWindowGuard(validation),
            sink);
        return new ClientIntentExecutor(planner, dispatcher);
    }

    private sealed class FixedVirtualKeyMapper : IVirtualKeyMapper
    {
        public bool TryMapScanCode(
            VirtualKey key,
            out byte scanCode)
        {
            scanCode = key == VirtualKey.Escape
                ? (byte)0x01
                : default;
            return scanCode != default;
        }
    }

    private sealed record UnsupportedIntent(ClientActionId Id)
        : ClientActionIntent(Id);

    private sealed class FixedWindowGuard : IClientWindowGuard
    {
        private readonly ClientWindowValidationResult result;

        public FixedWindowGuard(ClientWindowValidationResult result)
        {
            this.result = result;
        }

        public ClientWindowValidationResult Validate(
            ClientWindowTarget target) =>
            result;
    }

    private sealed class RecordingMessageSink : IWindowMessageSink
    {
        private readonly int failedAttemptIndex;
        private int attemptIndex;

        public RecordingMessageSink(int failedAttemptIndex = -1)
        {
            this.failedAttemptIndex = failedAttemptIndex;
        }

        public List<WindowInputMessage> Attempts { get; } = [];

        public bool TryPost(
            ClientWindowTarget target,
            WindowInputMessage message,
            out int nativeErrorCode)
        {
            Attempts.Add(message);
            if (attemptIndex++ == failedAttemptIndex)
            {
                nativeErrorCode = 5;
                return false;
            }

            nativeErrorCode = 0;
            return true;
        }
    }
}
