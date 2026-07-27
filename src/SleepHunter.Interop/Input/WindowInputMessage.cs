namespace SleepHunter.Interop.Input;

public enum ClientWindowMessage : uint
{
    Close = 0x0010,
    KeyDown = 0x0100,
    KeyUp = 0x0101,
    MouseMove = 0x0200,
    LeftButtonDown = 0x0201,
    LeftButtonUp = 0x0202
}

public readonly record struct WindowInputMessage
{
    public WindowInputMessage(
        ClientWindowMessage message,
        nuint wParam,
        nint lParam)
    {
        if (!Enum.IsDefined(message))
        {
            throw new ArgumentOutOfRangeException(
                nameof(message),
                message,
                "The client window message is not supported.");
        }

        Message = message;
        WParam = wParam;
        LParam = lParam;
    }

    public ClientWindowMessage Message { get; }

    public nuint WParam { get; }

    public nint LParam { get; }
}
