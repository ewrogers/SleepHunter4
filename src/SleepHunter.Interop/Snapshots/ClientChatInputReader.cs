using SleepHunter.Interop.Memory;

namespace SleepHunter.Interop.Snapshots;

internal static class ClientChatInputReader
{
    internal const int FocusedPaneOffset = 0x444;
    internal const int TimerHandlerCookieOffset = 0x120;
    internal const int VisibleOffset = 0x130;
    internal const uint LiveTimerHandlerCookie = 0x79736F62;

    public static bool TryRead(
        MemoryReadSession session,
        MemoryAddress inputManager,
        MemoryAddress chatInputPaneVtable,
        MemoryAddress tellReceiverInputPaneVtable,
        MemoryAddress tellInputPaneVtable,
        out bool isChatOpen,
        out ClientChatInputReadError? error)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (inputManager.IsNull ||
            chatInputPaneVtable.IsNull ||
            tellReceiverInputPaneVtable.IsNull ||
            tellInputPaneVtable.IsNull)
        {
            isChatOpen = false;
            error = new ClientChatInputReadError(
                ClientChatInputReadFailure.InvalidValue,
                "The input manager and chat pane vtables must be non-null.");
            return false;
        }

        if (!TryReadState(
                session,
                inputManager,
                out var initial,
                out var memoryError) ||
            !TryReadState(
                session,
                inputManager,
                out var confirmation,
                out memoryError))
        {
            isChatOpen = false;
            error = new ClientChatInputReadError(
                ClientChatInputReadFailure.MemoryReadFailed,
                "The focused input pane state could not be read.",
                memoryError);
            return false;
        }

        if (initial != confirmation)
        {
            isChatOpen = false;
            error = new ClientChatInputReadError(
                ClientChatInputReadFailure.StateChanged,
                "The focused input pane changed while it was being observed.");
            return false;
        }

        isChatOpen =
            !initial.FocusedPane.IsNull &&
            initial.TimerHandlerCookie == LiveTimerHandlerCookie &&
            initial.IsVisible &&
            (initial.Vtable == chatInputPaneVtable ||
             initial.Vtable == tellReceiverInputPaneVtable ||
             initial.Vtable == tellInputPaneVtable);
        error = null;
        return true;
    }

    private static bool TryReadState(
        MemoryReadSession session,
        MemoryAddress inputManager,
        out ClientChatInputState state,
        out MemoryReadError? error)
    {
        error = null;
        if (!inputManager.TryOffset(
                FocusedPaneOffset,
                out var focusedPaneAddress) ||
            !session.TryReadPointer(
                focusedPaneAddress,
                out var focusedPane,
                out error))
        {
            state = default;
            return false;
        }

        if (focusedPane.IsNull)
        {
            state = default;
            error = null;
            return true;
        }

        if (!focusedPane.TryOffset(
                TimerHandlerCookieOffset,
                out var cookieAddress) ||
            !focusedPane.TryOffset(
                VisibleOffset,
                out var visibleAddress) ||
            !session.TryReadPointer(
                focusedPane,
                out var vtable,
                out error) ||
            !session.TryReadUInt32(
                cookieAddress,
                out var timerHandlerCookie,
                out error) ||
            !session.TryReadByte(
                visibleAddress,
                out var visible,
                out error))
        {
            state = default;
            return false;
        }

        state = new ClientChatInputState(
            focusedPane,
            vtable,
            timerHandlerCookie,
            visible != 0);
        error = null;
        return true;
    }

    private readonly record struct ClientChatInputState(
        MemoryAddress FocusedPane,
        MemoryAddress Vtable,
        uint TimerHandlerCookie,
        bool IsVisible);
}

internal enum ClientChatInputReadFailure
{
    MemoryReadFailed,
    InvalidValue,
    StateChanged
}

internal sealed record ClientChatInputReadError(
    ClientChatInputReadFailure Failure,
    string Message,
    MemoryReadError? MemoryError = null);
