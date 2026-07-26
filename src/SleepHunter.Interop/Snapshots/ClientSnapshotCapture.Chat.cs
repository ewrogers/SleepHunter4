using SleepHunter.Interop.Mappings;

namespace SleepHunter.Interop.Snapshots;

public sealed partial class ClientSnapshotCapture
{
    private static bool TryReadIsChatOpen(
        MappedMemoryReader reader,
        out bool isChatOpen,
        out SnapshotCaptureError? error)
    {
        if (!reader.TryResolveAddress(
                InputManagerKey,
                out var inputManager,
                out var inputManagerError))
        {
            isChatOpen = false;
            error = MappingFailure(
                SnapshotSection.ClientState,
                InputManagerKey,
                inputManagerError);
            return false;
        }

        if (!reader.TryResolveAddress(
                ChatInputPaneVtableKey,
                out var chatInputPaneVtable,
                out var chatVtableError))
        {
            isChatOpen = false;
            error = MappingFailure(
                SnapshotSection.ClientState,
                ChatInputPaneVtableKey,
                chatVtableError);
            return false;
        }

        if (!reader.TryResolveAddress(
                TellReceiverInputPaneVtableKey,
                out var tellReceiverInputPaneVtable,
                out var receiverVtableError))
        {
            isChatOpen = false;
            error = MappingFailure(
                SnapshotSection.ClientState,
                TellReceiverInputPaneVtableKey,
                receiverVtableError);
            return false;
        }

        if (!reader.TryResolveAddress(
                TellInputPaneVtableKey,
                out var tellInputPaneVtable,
                out var tellVtableError))
        {
            isChatOpen = false;
            error = MappingFailure(
                SnapshotSection.ClientState,
                TellInputPaneVtableKey,
                tellVtableError);
            return false;
        }

        if (!ClientChatInputReader.TryRead(
                reader.Session,
                inputManager,
                chatInputPaneVtable,
                tellReceiverInputPaneVtable,
                tellInputPaneVtable,
                out isChatOpen,
                out var readError))
        {
            error = CreateChatInputReadError(readError!);
            return false;
        }

        if (!reader.TryResolveAddress(
                InputManagerKey,
                out var currentInputManager,
                out inputManagerError))
        {
            isChatOpen = false;
            error = MappingFailure(
                SnapshotSection.ClientState,
                InputManagerKey,
                inputManagerError);
            return false;
        }

        if (currentInputManager != inputManager)
        {
            isChatOpen = false;
            error = StateChanged(
                SnapshotSection.ClientState,
                InputManagerKey,
                "The input manager changed while chat input was observed.");
            return false;
        }

        error = null;
        return true;
    }

    private static SnapshotCaptureError CreateChatInputReadError(
        ClientChatInputReadError readError)
    {
        if (readError.Failure ==
            ClientChatInputReadFailure.MemoryReadFailed)
        {
            return MappingFailure(
                SnapshotSection.ClientState,
                InputManagerKey,
                new MappedMemoryReadError(
                    MappedMemoryReadFailure.ValueReadFailed,
                    InputManagerKey,
                    ActualKind: MemoryValueKind.Unsigned32,
                    MemoryError: readError.MemoryError));
        }

        if (readError.Failure ==
            ClientChatInputReadFailure.StateChanged)
        {
            return StateChanged(
                SnapshotSection.ClientState,
                InputManagerKey,
                readError.Message);
        }

        return InvalidValue(
            SnapshotSection.ClientState,
            InputManagerKey,
            readError.Message);
    }
}
