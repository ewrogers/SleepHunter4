using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Text;
using SleepHunter.Interop.Memory;
using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Interop.Snapshots;

internal static class ClientMessageDialogReader
{
    internal const int EntrySize = 0x0C;
    internal const int MaximumEntryCount = 4096;
    internal const int MaximumControlCount = 512;
    internal const uint RegisteredFlag = 0x02;

    private const int EventListOffset = 0x64;
    private const int EventListHeaderSize = 0x0C;
    private const int PaneVisibleOffset = 0x130;
    private const int PaneRegistrationFlagsOffset = 0x188;
    private const int DialogControlsOffset = 0x594;
    private const int ListCountOffset = 0x14;
    private const int ContentControlIndex = 1;
    private const int ContentTextPaneOffset = 0x19C;
    private const int TextCharacterListOffset = 0x1BC;
    private const int TextListHeaderSize = 0x08;

    private static readonly Encoding StrictAscii = Encoding.GetEncoding(
        Encoding.ASCII.CodePage,
        EncoderFallback.ExceptionFallback,
        DecoderFallback.ExceptionFallback);

    public static bool TryRead(
        MemoryReadSession session,
        MemoryAddress dispatcher,
        MemoryAddress expectedVtable,
        out MessageDialogsSnapshot dialogs,
        out ClientMessageDialogReadError? error)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (dispatcher.IsNull || expectedVtable.IsNull)
        {
            dialogs = MessageDialogsSnapshot.Empty;
            error = new ClientMessageDialogReadError(
                ClientMessageDialogReadFailure.InvalidValue,
                "The event dispatcher and dialog vtable must be non-null.");
            return false;
        }

        if (!TryReadEventList(
                session,
                dispatcher,
                out var eventList,
                out var memoryError))
        {
            return MemoryFailure(
                "The active event registration list could not be read.",
                memoryError,
                out dialogs,
                out error);
        }

        if (eventList.Count < 0 ||
            eventList.Capacity < eventList.Count ||
            eventList.Capacity > MaximumEntryCount)
        {
            dialogs = MessageDialogsSnapshot.Empty;
            error = new ClientMessageDialogReadError(
                ClientMessageDialogReadFailure.InvalidValue,
                $"The active event registration list has invalid count {eventList.Count} and capacity {eventList.Capacity}.");
            return false;
        }

        if (eventList.Count == 0)
        {
            if (!TryReadEventList(
                    session,
                    dispatcher,
                    out var confirmation,
                    out memoryError))
            {
                return MemoryFailure(
                    "The active event registration list could not be confirmed.",
                    memoryError,
                    out dialogs,
                    out error);
            }

            if (confirmation != eventList)
            {
                return CollectionChanged(out dialogs, out error);
            }

            dialogs = MessageDialogsSnapshot.Empty;
            error = null;
            return true;
        }

        if (eventList.Entries.IsNull)
        {
            dialogs = MessageDialogsSnapshot.Empty;
            error = new ClientMessageDialogReadError(
                ClientMessageDialogReadFailure.InvalidValue,
                "The active event registration list has a null entry array.");
            return false;
        }

        var entries = new byte[checked(eventList.Count * EntrySize)];
        if (!session.TryRead(eventList.Entries, entries, out memoryError))
        {
            return MemoryFailure(
                "The active event registration entries could not be read.",
                memoryError,
                out dialogs,
                out error);
        }

        var builder = ImmutableArray.CreateBuilder<MessageDialogSnapshot>();
        for (var index = 0; index < eventList.Count; index++)
        {
            var record = entries.AsSpan(index * EntrySize, EntrySize);
            var pane = new MemoryAddress(
                BinaryPrimitives.ReadUInt32LittleEndian(record));
            var treeDepth = BinaryPrimitives.ReadUInt32LittleEndian(record[4..]);
            var identity = BinaryPrimitives.ReadUInt32LittleEndian(record[8..]);

            if (pane.IsNull)
            {
                dialogs = MessageDialogsSnapshot.Empty;
                error = new ClientMessageDialogReadError(
                    ClientMessageDialogReadFailure.InvalidValue,
                    $"Active event registration {index} has a null pane.");
                return false;
            }

            if (!session.TryReadPointer(
                    pane,
                    out var paneVtable,
                    out memoryError))
            {
                return MemoryFailure(
                    "An active event pane could not be classified.",
                    memoryError,
                    out dialogs,
                    out error);
            }

            if (paneVtable != expectedVtable)
            {
                continue;
            }

            // The supported English client can leave the inherited +0x04
            // cookie at zero on a displayed message pane. Stable event-tree
            // membership, registration, and visibility are the verified
            // lifetime gates.
            if (!TryReadDialogOpenState(
                    session,
                    pane,
                    out var isVisible,
                    out var isRegistered,
                    out memoryError))
            {
                return MemoryFailure(
                    "A message dialog pane state could not be read.",
                    memoryError,
                    out dialogs,
                    out error);
            }

            if (!isRegistered || !isVisible)
            {
                continue;
            }

            if (!TryReadText(
                    session,
                    pane,
                    out var text,
                    out var textError))
            {
                dialogs = MessageDialogsSnapshot.Empty;
                error = textError;
                return false;
            }

            builder.Add(
                new MessageDialogSnapshot(
                    treeDepth,
                    identity,
                    text));
        }

        if (!TryReadEventList(
                session,
                dispatcher,
                out var currentEventList,
                out memoryError))
        {
            return MemoryFailure(
                "The active event registration list could not be confirmed.",
                memoryError,
                out dialogs,
                out error);
        }

        if (currentEventList != eventList)
        {
            return CollectionChanged(out dialogs, out error);
        }

        var confirmationEntries = new byte[entries.Length];
        if (!session.TryRead(
                currentEventList.Entries,
                confirmationEntries,
                out memoryError))
        {
            return MemoryFailure(
                "The active event registration entries could not be confirmed.",
                memoryError,
                out dialogs,
                out error);
        }

        if (!confirmationEntries.AsSpan().SequenceEqual(entries))
        {
            return CollectionChanged(out dialogs, out error);
        }

        dialogs = new MessageDialogsSnapshot(builder);
        error = null;
        return true;
    }

    private static bool TryReadDialogOpenState(
        MemoryReadSession session,
        MemoryAddress pane,
        out bool isVisible,
        out bool isRegistered,
        out MemoryReadError? error)
    {
        error = null;
        if (!pane.TryOffset(PaneVisibleOffset, out var visibleAddress) ||
            !session.TryReadByte(
                visibleAddress,
                out var visible,
                out error) ||
            !pane.TryOffset(
                PaneRegistrationFlagsOffset,
                out var registrationAddress) ||
            !session.TryReadUInt32(
                registrationAddress,
                out var registrationFlags,
                out error))
        {
            isVisible = false;
            isRegistered = false;
            return false;
        }

        isVisible = visible != 0;
        isRegistered = (registrationFlags & RegisteredFlag) != 0;
        return true;
    }

    private static bool TryReadText(
        MemoryReadSession session,
        MemoryAddress pane,
        out string text,
        out ClientMessageDialogReadError? error)
    {
        if (!TryReadPointerAt(
                session,
                pane,
                DialogControlsOffset,
                out var controls,
                out var memoryError))
        {
            return TextMemoryFailure(
                "The dialog control list could not be resolved.",
                memoryError,
                out text,
                out error);
        }

        if (controls.IsNull)
        {
            return TextInvalidValue(
                "The dialog has a null control list.",
                out text,
                out error);
        }

        Span<byte> listHeader = stackalloc byte[TextListHeaderSize];
        if (!controls.TryOffset(ListCountOffset, out var listHeaderAddress) ||
            !session.TryRead(listHeaderAddress, listHeader, out memoryError))
        {
            return TextMemoryFailure(
                "The dialog control list header could not be read.",
                memoryError,
                out text,
                out error);
        }

        var controlCount = BinaryPrimitives.ReadInt32LittleEndian(listHeader);
        var controlArray = new MemoryAddress(
            BinaryPrimitives.ReadUInt32LittleEndian(listHeader[4..]));
        if (controlCount < 2 ||
            controlCount > MaximumControlCount ||
            controlArray.IsNull)
        {
            text = string.Empty;
            error = new ClientMessageDialogReadError(
                ClientMessageDialogReadFailure.InvalidValue,
                $"The dialog control list has invalid count {controlCount} or storage.");
            return false;
        }

        if (!TryReadPointerAt(
                session,
                controlArray,
                ContentControlIndex * sizeof(uint),
                out var contentControl,
                out memoryError))
        {
            return TextMemoryFailure(
                "The dialog content control could not be read.",
                memoryError,
                out text,
                out error);
        }

        if (contentControl.IsNull)
        {
            return TextInvalidValue(
                "The dialog has a null content control.",
                out text,
                out error);
        }

        if (!TryReadPointerAt(
                session,
                contentControl,
                ContentTextPaneOffset,
                out var textPane,
                out memoryError))
        {
            return TextMemoryFailure(
                "The dialog text pane pointer could not be read.",
                memoryError,
                out text,
                out error);
        }

        if (textPane.IsNull)
        {
            return TextInvalidValue(
                "The dialog has a null text pane.",
                out text,
                out error);
        }

        if (!TryReadPointerAt(
                session,
                textPane,
                TextCharacterListOffset,
                out var characterList,
                out memoryError))
        {
            return TextMemoryFailure(
                "The dialog character list pointer could not be read.",
                memoryError,
                out text,
                out error);
        }

        if (characterList.IsNull)
        {
            return TextInvalidValue(
                "The dialog has a null character list.",
                out text,
                out error);
        }

        if (!characterList.TryOffset(
                ListCountOffset,
                out var characterListHeaderAddress) ||
            !session.TryRead(
                characterListHeaderAddress,
                listHeader,
                out memoryError))
        {
            return TextMemoryFailure(
                "The dialog character list header could not be read.",
                memoryError,
                out text,
                out error);
        }

        var textLength = BinaryPrimitives.ReadInt32LittleEndian(listHeader);
        var textBytes = new MemoryAddress(
            BinaryPrimitives.ReadUInt32LittleEndian(listHeader[4..]));
        if (textLength < 0 ||
            textLength > session.Limits.MaximumStringBytes ||
            textLength > 0 && textBytes.IsNull)
        {
            text = string.Empty;
            error = new ClientMessageDialogReadError(
                ClientMessageDialogReadFailure.InvalidValue,
                $"The dialog text has invalid byte length {textLength} or storage.");
            return false;
        }

        if (textLength == 0)
        {
            text = string.Empty;
            error = null;
            return true;
        }

        var bytes = new byte[textLength];
        if (!session.TryRead(textBytes, bytes, out memoryError))
        {
            return TextMemoryFailure(
                "The dialog text bytes could not be read.",
                memoryError,
                out text,
                out error);
        }

        try
        {
            text = StrictAscii
                .GetString(bytes)
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Replace('\r', '\n');
            error = null;
            return true;
        }
        catch (DecoderFallbackException)
        {
            text = string.Empty;
            error = new ClientMessageDialogReadError(
                ClientMessageDialogReadFailure.InvalidEncoding,
                "The dialog text is not valid client ASCII text.");
            return false;
        }
    }

    private static bool TryReadEventList(
        MemoryReadSession session,
        MemoryAddress dispatcher,
        out EventList eventList,
        out MemoryReadError? error)
    {
        Span<byte> header = stackalloc byte[EventListHeaderSize];
        if (!dispatcher.TryOffset(EventListOffset, out var headerAddress))
        {
            eventList = default;
            error = null;
            return false;
        }

        if (!session.TryRead(headerAddress, header, out error))
        {
            eventList = default;
            return false;
        }

        eventList = new EventList(
            new MemoryAddress(
                BinaryPrimitives.ReadUInt32LittleEndian(header)),
            BinaryPrimitives.ReadInt32LittleEndian(header[4..]),
            BinaryPrimitives.ReadInt32LittleEndian(header[8..]));
        return true;
    }

    private static bool TryReadPointerAt(
        MemoryReadSession session,
        MemoryAddress owner,
        int offset,
        out MemoryAddress pointer,
        out MemoryReadError? error)
    {
        if (!owner.TryOffset(offset, out var pointerAddress))
        {
            pointer = default;
            error = null;
            return false;
        }

        return session.TryReadPointer(pointerAddress, out pointer, out error);
    }

    private static bool MemoryFailure(
        string message,
        MemoryReadError? memoryError,
        out MessageDialogsSnapshot dialogs,
        out ClientMessageDialogReadError? error)
    {
        dialogs = MessageDialogsSnapshot.Empty;
        error = new ClientMessageDialogReadError(
            ClientMessageDialogReadFailure.MemoryReadFailed,
            message,
            memoryError);
        return false;
    }

    private static bool TextMemoryFailure(
        string message,
        MemoryReadError? memoryError,
        out string text,
        out ClientMessageDialogReadError? error)
    {
        text = string.Empty;
        error = new ClientMessageDialogReadError(
            ClientMessageDialogReadFailure.MemoryReadFailed,
            message,
            memoryError);
        return false;
    }

    private static bool TextInvalidValue(
        string message,
        out string text,
        out ClientMessageDialogReadError? error)
    {
        text = string.Empty;
        error = new ClientMessageDialogReadError(
            ClientMessageDialogReadFailure.InvalidValue,
            message);
        return false;
    }

    private static bool CollectionChanged(
        out MessageDialogsSnapshot dialogs,
        out ClientMessageDialogReadError? error)
    {
        dialogs = MessageDialogsSnapshot.Empty;
        error = new ClientMessageDialogReadError(
            ClientMessageDialogReadFailure.CollectionChanged,
            "The active event registration list changed during capture.");
        return false;
    }

    private readonly record struct EventList(
        MemoryAddress Entries,
        int Count,
        int Capacity);
}

internal sealed record ClientMessageDialogReadError(
    ClientMessageDialogReadFailure Failure,
    string Message,
    MemoryReadError? MemoryError = null);

internal enum ClientMessageDialogReadFailure
{
    MemoryReadFailed,
    CollectionChanged,
    InvalidValue,
    InvalidEncoding
}
