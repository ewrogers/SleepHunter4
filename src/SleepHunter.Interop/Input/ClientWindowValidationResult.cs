namespace SleepHunter.Interop.Input;

public enum ClientWindowValidationFailure
{
    None,
    WindowUnavailable,
    ProcessMismatch,
    ClientAreaUnavailable,
    ClientAreaChanged
}

public sealed record ClientWindowValidationResult
{
    public ClientWindowValidationResult(
        ClientWindowValidationFailure failure,
        string message,
        int nativeErrorCode = 0)
    {
        if (!Enum.IsDefined(failure))
        {
            throw new ArgumentOutOfRangeException(
                nameof(failure),
                failure,
                "The client window validation failure is not supported.");
        }

        if (failure == ClientWindowValidationFailure.None)
        {
            throw new ArgumentException(
                "Successful client window validation must use the shared valid result.",
                nameof(failure));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Failure = failure;
        Message = message.Trim();
        NativeErrorCode = nativeErrorCode;
    }

    private ClientWindowValidationResult()
    {
        Failure = ClientWindowValidationFailure.None;
        Message = string.Empty;
    }

    public static ClientWindowValidationResult Valid { get; } = new();

    public ClientWindowValidationFailure Failure { get; }

    public string Message { get; }

    public int NativeErrorCode { get; }

    public bool IsValid => Failure == ClientWindowValidationFailure.None;
}

public interface IClientWindowGuard
{
    ClientWindowValidationResult Validate(ClientWindowTarget target);
}
