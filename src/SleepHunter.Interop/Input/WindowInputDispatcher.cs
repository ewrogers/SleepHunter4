namespace SleepHunter.Interop.Input;

public sealed class WindowInputDispatcher
{
    private readonly IClientWindowGuard guard;
    private readonly IWindowMessageSink sink;

    public WindowInputDispatcher(
        IClientWindowGuard guard,
        IWindowMessageSink sink)
    {
        ArgumentNullException.ThrowIfNull(guard);
        ArgumentNullException.ThrowIfNull(sink);

        this.guard = guard;
        this.sink = sink;
    }

    public WindowInputDispatchResult Dispatch(
        ClientWindowTarget target,
        WindowInputPlan plan)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(plan);

        var validation = guard.Validate(target);
        if (!validation.IsValid)
        {
            return new WindowInputDispatchResult(
                WindowInputDispatchStatus.Rejected,
                postedMessageCount: 0,
                postedCleanupMessageCount: 0,
                validation: validation);
        }

        for (var index = 0; index < plan.Messages.Length; index++)
        {
            if (sink.TryPost(
                    target,
                    plan.Messages[index],
                    out var nativeErrorCode))
            {
                continue;
            }

            var cleanupCount = PostCleanup(target, plan);
            return new WindowInputDispatchResult(
                index == 0
                    ? WindowInputDispatchStatus.Failed
                    : WindowInputDispatchStatus.PartiallyIssued,
                postedMessageCount: index,
                postedCleanupMessageCount: cleanupCount,
                failedMessageIndex: index,
                nativeErrorCode);
        }

        return new WindowInputDispatchResult(
            WindowInputDispatchStatus.Issued,
            postedMessageCount: plan.Messages.Length,
            postedCleanupMessageCount: 0);
    }

    private int PostCleanup(
        ClientWindowTarget target,
        WindowInputPlan plan)
    {
        var postedCount = 0;
        foreach (var message in plan.CleanupMessages)
        {
            if (sink.TryPost(target, message, out _))
            {
                postedCount++;
            }
        }

        return postedCount;
    }
}
