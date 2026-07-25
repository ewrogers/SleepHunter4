using SleepHunter.Interop.Input;
using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Interop.Tests.Input;

public sealed class WindowInputDispatcherTests
{
    private static readonly ClientWindowTarget Target = new(
        new ClientIdentity("process:1234", "USDA 7.41"),
        processId: 1234,
        windowHandle: new nint(0x1234),
        clientWidth: 640,
        clientHeight: 480);

    private static readonly WindowInputMessage KeyDown = new(
        ClientWindowMessage.KeyDown,
        wParam: 0x20,
        lParam: new nint(0x00390001));

    private static readonly WindowInputMessage KeyUp = new(
        ClientWindowMessage.KeyUp,
        wParam: 0x20,
        lParam: new nint(unchecked((int)0xC0390001)));

    private static readonly WindowInputMessage MouseMove = new(
        ClientWindowMessage.MouseMove,
        wParam: 0,
        lParam: new nint(0x00A0013B));

    [Test]
    public void ShouldRejectInvalidWindowBeforePostingInput()
    {
        var validation = new ClientWindowValidationResult(
            ClientWindowValidationFailure.ProcessMismatch,
            "The window owner changed.");
        var sink = new RecordingMessageSink();
        var dispatcher = new WindowInputDispatcher(
            new FixedWindowGuard(validation),
            sink);

        var result = dispatcher.Dispatch(
            Target,
            new WindowInputPlan([KeyDown, KeyUp]));

        Assert.Multiple(() =>
        {
            Assert.That(
                result.Status,
                Is.EqualTo(WindowInputDispatchStatus.Rejected));
            Assert.That(
                result.Validation?.Failure,
                Is.EqualTo(ClientWindowValidationFailure.ProcessMismatch));
            Assert.That(result.PostedMessageCount, Is.Zero);
            Assert.That(sink.Attempts, Is.Empty);
        });
    }

    [Test]
    public void ShouldPostACompleteInputPlanInOrder()
    {
        var sink = new RecordingMessageSink();
        var dispatcher = new WindowInputDispatcher(
            new FixedWindowGuard(ClientWindowValidationResult.Valid),
            sink);
        var plan = new WindowInputPlan([MouseMove, KeyDown, KeyUp]);

        var result = dispatcher.Dispatch(Target, plan);

        Assert.Multiple(() =>
        {
            Assert.That(
                result.Status,
                Is.EqualTo(WindowInputDispatchStatus.Issued));
            Assert.That(result.PostedMessageCount, Is.EqualTo(3));
            Assert.That(result.PostedCleanupMessageCount, Is.Zero);
            Assert.That(sink.Attempts, Is.EqualTo(plan.Messages));
        });
    }

    [Test]
    public void ShouldReportPartialIssuanceAndPostCleanup()
    {
        var cleanupMouseUp = new WindowInputMessage(
            ClientWindowMessage.LeftButtonUp,
            wParam: 0,
            lParam: new nint(0x00A0013B));
        var sink = new RecordingMessageSink(
            failedAttemptIndex: 2,
            failureCode: 1400);
        var dispatcher = new WindowInputDispatcher(
            new FixedWindowGuard(ClientWindowValidationResult.Valid),
            sink);
        var plan = new WindowInputPlan(
            [MouseMove, KeyDown, KeyUp],
            [cleanupMouseUp, KeyUp]);

        var result = dispatcher.Dispatch(Target, plan);

        Assert.Multiple(() =>
        {
            Assert.That(
                result.Status,
                Is.EqualTo(WindowInputDispatchStatus.PartiallyIssued));
            Assert.That(result.PostedMessageCount, Is.EqualTo(2));
            Assert.That(result.PostedCleanupMessageCount, Is.EqualTo(2));
            Assert.That(result.FailedMessageIndex, Is.EqualTo(2));
            Assert.That(result.NativeErrorCode, Is.EqualTo(1400));
            Assert.That(
                sink.Attempts,
                Is.EqualTo(
                    new[]
                    {
                        MouseMove,
                        KeyDown,
                        KeyUp,
                        cleanupMouseUp,
                        KeyUp
                    }));
        });
    }

    [Test]
    public void ShouldReportFailureWhenTheFirstMessageIsNotPosted()
    {
        var sink = new RecordingMessageSink(
            failedAttemptIndex: 0,
            failureCode: 5);
        var dispatcher = new WindowInputDispatcher(
            new FixedWindowGuard(ClientWindowValidationResult.Valid),
            sink);

        var result = dispatcher.Dispatch(
            Target,
            new WindowInputPlan([KeyDown, KeyUp], [KeyUp]));

        Assert.Multiple(() =>
        {
            Assert.That(
                result.Status,
                Is.EqualTo(WindowInputDispatchStatus.Failed));
            Assert.That(result.PostedMessageCount, Is.Zero);
            Assert.That(result.PostedCleanupMessageCount, Is.EqualTo(1));
            Assert.That(result.FailedMessageIndex, Is.Zero);
            Assert.That(result.NativeErrorCode, Is.EqualTo(5));
            Assert.That(sink.Attempts, Is.EqualTo(new[] { KeyDown, KeyUp }));
        });
    }

    [Test]
    public void ShouldValidateTargetsMessagesAndPlanBounds()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = new ClientWindowTarget(
                    Target.Client,
                    processId: 0,
                    Target.WindowHandle,
                    Target.ClientWidth,
                    Target.ClientHeight));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = new ClientWindowTarget(
                    Target.Client,
                    Target.ProcessId,
                    windowHandle: nint.Zero,
                    Target.ClientWidth,
                    Target.ClientHeight));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = new WindowInputMessage(
                    (ClientWindowMessage)uint.MaxValue,
                    wParam: 0,
                    lParam: 0));
            Assert.Throws<ArgumentException>(
                () => _ = new WindowInputPlan([]));
            Assert.Throws<ArgumentException>(
                () => _ = new WindowInputPlan(
                    Enumerable.Repeat(
                        KeyDown,
                        WindowInputPlan.MaximumMessageCount + 1)));
        });
    }

    [Test]
    public void ShouldRejectAnInvalidNativeWindowHandle()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Ignore("The native client window guard requires Windows.");
        }

        var guard = new WindowsClientWindowGuard();
        var target = new ClientWindowTarget(
            Target.Client,
            Target.ProcessId,
            windowHandle: new nint(1),
            Target.ClientWidth,
            Target.ClientHeight);

        var result = guard.Validate(target);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(ClientWindowValidationFailure.WindowUnavailable));
        });
    }

    private sealed class FixedWindowGuard : IClientWindowGuard
    {
        private readonly ClientWindowValidationResult result;

        public FixedWindowGuard(ClientWindowValidationResult result)
        {
            ArgumentNullException.ThrowIfNull(result);
            this.result = result;
        }

        public ClientWindowValidationResult Validate(
            ClientWindowTarget target) =>
            result;
    }

    private sealed class RecordingMessageSink : IWindowMessageSink
    {
        private readonly int failedAttemptIndex;
        private readonly int failureCode;
        private int attemptIndex;

        public RecordingMessageSink(
            int failedAttemptIndex = -1,
            int failureCode = 0)
        {
            this.failedAttemptIndex = failedAttemptIndex;
            this.failureCode = failureCode;
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
                nativeErrorCode = failureCode;
                return false;
            }

            nativeErrorCode = 0;
            return true;
        }
    }
}
