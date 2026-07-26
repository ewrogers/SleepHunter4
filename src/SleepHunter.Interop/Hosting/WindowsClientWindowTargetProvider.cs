using SleepHunter.Interop.Input;
using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Interop.Hosting;

public sealed class WindowsClientWindowTargetProvider
    : IClientWindowTargetProvider
{
    private readonly nint windowHandle;
    private readonly int processId;

    public WindowsClientWindowTargetProvider(
        ClientIdentity client,
        int processId,
        nint windowHandle)
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

        Client = client;
        this.processId = processId;
        this.windowHandle = windowHandle;
    }

    public ClientIdentity Client { get; }

    public bool TryGetTarget(out ClientWindowTarget? target)
    {
        target = null;
        if (!NativeMethods.IsWindow(windowHandle))
        {
            return false;
        }

        _ = NativeMethods.GetWindowThreadProcessId(
            windowHandle,
            out var currentProcessId);
        if (currentProcessId != processId ||
            !NativeMethods.GetClientRect(windowHandle, out var clientRectangle))
        {
            return false;
        }

        if (clientRectangle.Width is <= 0 or > short.MaxValue ||
            clientRectangle.Height is <= 0 or > short.MaxValue)
        {
            return false;
        }

        target = new ClientWindowTarget(
            Client,
            processId,
            windowHandle,
            clientRectangle.Width,
            clientRectangle.Height);
        return true;
    }
}
