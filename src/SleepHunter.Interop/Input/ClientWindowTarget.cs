using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Interop.Input;

public sealed record ClientWindowTarget
{
    public ClientWindowTarget(
        ClientIdentity client,
        int processId,
        nint windowHandle,
        int clientWidth,
        int clientHeight)
    {
        ArgumentNullException.ThrowIfNull(client);

        if (processId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(processId),
                processId,
                "The client process identifier must be positive.");
        }

        if (windowHandle == nint.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(windowHandle),
                windowHandle,
                "The client window handle cannot be zero.");
        }

        ValidateClientDimension(clientWidth, nameof(clientWidth));
        ValidateClientDimension(clientHeight, nameof(clientHeight));

        Client = client;
        ProcessId = processId;
        WindowHandle = windowHandle;
        ClientWidth = clientWidth;
        ClientHeight = clientHeight;
    }

    public ClientIdentity Client { get; }

    public int ProcessId { get; }

    public nint WindowHandle { get; }

    public int ClientWidth { get; }

    public int ClientHeight { get; }

    private static void ValidateClientDimension(
        int dimension,
        string parameterName)
    {
        if (dimension is <= 0 or > short.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                dimension,
                $"Client dimensions must be between 1 and {short.MaxValue}.");
        }
    }
}
