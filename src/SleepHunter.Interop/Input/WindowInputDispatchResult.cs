namespace SleepHunter.Interop.Input;

public enum WindowInputDispatchStatus
{
    Issued,
    Rejected,
    Failed,
    PartiallyIssued
}

public sealed record WindowInputDispatchResult
{
    public WindowInputDispatchResult(
        WindowInputDispatchStatus status,
        int postedMessageCount,
        int postedCleanupMessageCount,
        int? failedMessageIndex = null,
        int nativeErrorCode = 0,
        ClientWindowValidationResult? validation = null)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "The input dispatch status is not supported.");
        }

        if (postedMessageCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(postedMessageCount),
                postedMessageCount,
                "The posted message count cannot be negative.");
        }

        if (postedCleanupMessageCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(postedCleanupMessageCount),
                postedCleanupMessageCount,
                "The posted cleanup message count cannot be negative.");
        }

        if (failedMessageIndex < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(failedMessageIndex),
                failedMessageIndex,
                "The failed message index cannot be negative.");
        }

        if (status == WindowInputDispatchStatus.Issued &&
            (postedMessageCount <= 0 ||
             postedCleanupMessageCount != 0 ||
             failedMessageIndex is not null ||
             validation is not null))
        {
            throw new ArgumentException(
                "Issued input must contain posted messages and no failure.");
        }

        if (status == WindowInputDispatchStatus.Rejected &&
            (validation is not { IsValid: false } ||
             postedMessageCount != 0 ||
             postedCleanupMessageCount != 0 ||
             failedMessageIndex is not null))
        {
            throw new ArgumentException(
                "Rejected input must report a window validation failure before posting.");
        }

        var hasDispatchFailure =
            status is WindowInputDispatchStatus.Failed or
                WindowInputDispatchStatus.PartiallyIssued;
        if (hasDispatchFailure &&
            (failedMessageIndex is null || validation is not null))
        {
            throw new ArgumentException(
                "Failed input must report the failed message without a window validation failure.");
        }

        if (hasDispatchFailure &&
            failedMessageIndex != postedMessageCount)
        {
            throw new ArgumentException(
                "The failed message must immediately follow the posted intended messages.",
                nameof(failedMessageIndex));
        }

        if ((status == WindowInputDispatchStatus.Failed) !=
            (postedMessageCount == 0 && failedMessageIndex is not null))
        {
            throw new ArgumentException(
                "Failed input cannot have posted an intended message.");
        }

        if ((status == WindowInputDispatchStatus.PartiallyIssued) !=
            (postedMessageCount > 0 && failedMessageIndex is not null))
        {
            throw new ArgumentException(
                "Partially issued input must have posted at least one intended message.");
        }

        Status = status;
        PostedMessageCount = postedMessageCount;
        PostedCleanupMessageCount = postedCleanupMessageCount;
        FailedMessageIndex = failedMessageIndex;
        NativeErrorCode = nativeErrorCode;
        Validation = validation;
    }

    public WindowInputDispatchStatus Status { get; }

    public int PostedMessageCount { get; }

    public int PostedCleanupMessageCount { get; }

    public int? FailedMessageIndex { get; }

    public int NativeErrorCode { get; }

    public ClientWindowValidationResult? Validation { get; }
}
