using SleepHunter.Interop.Mappings;
using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Interop.Snapshots;

public sealed partial class ClientSnapshotCapture
{
    private static bool TryReadMessageDialogs(
        MappedMemoryReader reader,
        out MessageDialogsSnapshot? dialogs,
        out SnapshotCaptureError? error,
        out SnapshotQuality failureQuality)
    {
        if (!reader.TryResolveAddress(
                ActiveEventDispatcherKey,
                out var dispatcher,
                out var dispatcherError))
        {
            dialogs = null;
            error = MappingFailure(
                SnapshotSection.MessageDialogs,
                ActiveEventDispatcherKey,
                dispatcherError);
            failureQuality = SnapshotQuality.Partial;
            return false;
        }

        if (!reader.TryResolveAddress(
                WindowMessageDialogPaneVtableKey,
                out var expectedVtable,
                out var vtableError))
        {
            dialogs = null;
            error = MappingFailure(
                SnapshotSection.MessageDialogs,
                WindowMessageDialogPaneVtableKey,
                vtableError);
            failureQuality = SnapshotQuality.Partial;
            return false;
        }

        if (!ClientMessageDialogReader.TryRead(
                reader.Session,
                dispatcher,
                expectedVtable,
                out dialogs,
                out var readError))
        {
            error = CreateDialogReadError(readError!);
            failureQuality =
                readError!.Failure ==
                ClientMessageDialogReadFailure.MemoryReadFailed
                    ? SnapshotQuality.Partial
                    : SnapshotQuality.Incoherent;
            return false;
        }

        if (!reader.TryResolveAddress(
                ActiveEventDispatcherKey,
                out var currentDispatcher,
                out dispatcherError))
        {
            dialogs = null;
            error = MappingFailure(
                SnapshotSection.MessageDialogs,
                ActiveEventDispatcherKey,
                dispatcherError);
            failureQuality = SnapshotQuality.Partial;
            return false;
        }

        if (currentDispatcher != dispatcher)
        {
            dialogs = null;
            error = StateChanged(
                SnapshotSection.MessageDialogs,
                ActiveEventDispatcherKey,
                "The active event dispatcher changed during snapshot capture.");
            failureQuality = SnapshotQuality.Incoherent;
            return false;
        }

        error = null;
        failureQuality = SnapshotQuality.Unknown;
        return true;
    }

    private static SnapshotCaptureError CreateDialogReadError(
        ClientMessageDialogReadError readError)
    {
        if (readError.Failure ==
            ClientMessageDialogReadFailure.MemoryReadFailed)
        {
            return MappingFailure(
                SnapshotSection.MessageDialogs,
                ActiveEventDispatcherKey,
                new MappedMemoryReadError(
                    MappedMemoryReadFailure.ValueReadFailed,
                    ActiveEventDispatcherKey,
                    ActualKind: MemoryValueKind.Unsigned32,
                    MemoryError: readError.MemoryError));
        }

        if (readError.Failure ==
            ClientMessageDialogReadFailure.CollectionChanged)
        {
            return StateChanged(
                SnapshotSection.MessageDialogs,
                ActiveEventDispatcherKey,
                readError.Message);
        }

        return InvalidValue(
            SnapshotSection.MessageDialogs,
            ActiveEventDispatcherKey,
            readError.Message);
    }
}
