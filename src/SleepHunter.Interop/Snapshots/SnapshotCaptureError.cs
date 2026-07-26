using SleepHunter.Interop.Mappings;

namespace SleepHunter.Interop.Snapshots;

public sealed record SnapshotCaptureError
{
    public SnapshotCaptureError(
        SnapshotSection section,
        SnapshotCaptureFailure failure,
        string message,
        string? variableKey = null,
        MappedMemoryReadError? readError = null)
    {
        if (!Enum.IsDefined(section))
        {
            throw new ArgumentOutOfRangeException(
                nameof(section),
                section,
                "The snapshot section is not supported.");
        }

        if (!Enum.IsDefined(failure))
        {
            throw new ArgumentOutOfRangeException(
                nameof(failure),
                failure,
                "The snapshot capture failure is not supported.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Section = section;
        Failure = failure;
        Message = message;
        VariableKey = string.IsNullOrWhiteSpace(variableKey)
            ? null
            : variableKey.Trim();
        ReadError = readError;
    }

    public SnapshotSection Section { get; }

    public SnapshotCaptureFailure Failure { get; }

    public string Message { get; }

    public string? VariableKey { get; }

    public MappedMemoryReadError? ReadError { get; }
}
