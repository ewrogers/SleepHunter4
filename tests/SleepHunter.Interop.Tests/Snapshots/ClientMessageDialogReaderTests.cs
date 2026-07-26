using System.Buffers.Binary;
using System.Text;
using SleepHunter.Interop.Memory;
using SleepHunter.Interop.Snapshots;
using SleepHunter.Interop.Tests.Memory;
using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Interop.Tests.Snapshots;

public sealed class ClientMessageDialogReaderTests
{
    private const ulong DispatcherAddress = 0x1000;
    private const ulong EntriesAddress = 0x2000;
    private const ulong ExpectedVtable = 0x672A84;
    private const uint LiveCookie = 0x79736F62;

    [Test]
    public void ShouldCaptureEveryVisibleRegisteredDialogAndDecodeAsciiText()
    {
        var source = new MemoryImageSource();
        WriteEventList(
            source,
            [
                Entry(0x3000, treeDepth: 2, identity: 11),
                Entry(0x4000, treeDepth: 1, identity: 12),
                Entry(0x5000, treeDepth: 3, identity: 13)
            ]);
        WriteDialog(
            source,
            paneAddress: 0x3000,
            controlsAddress: 0x6000,
            controlArrayAddress: 0x6100,
            contentControlAddress: 0x6200,
            textPaneAddress: 0x6300,
            characterListAddress: 0x6400,
            textAddress: 0x6500,
            "Sense\rEidolon",
            liveCookie: 0);
        WriteDialog(
            source,
            paneAddress: 0x4000,
            controlsAddress: 0x7000,
            controlArrayAddress: 0x7100,
            contentControlAddress: 0x7200,
            textPaneAddress: 0x7300,
            characterListAddress: 0x7400,
            textAddress: 0x7500,
            "Peek");
        WritePaneState(
            source,
            paneAddress: 0x5000,
            vtable: 0x12345678,
            LiveCookie,
            isVisible: true,
            isRegistered: true);
        var session = new MemoryReadSession(
            source,
            MemoryReadLimits.Client32Bit);

        var succeeded = ClientMessageDialogReader.TryRead(
            session,
            new MemoryAddress(DispatcherAddress),
            new MemoryAddress(ExpectedVtable),
            out var dialogs,
            out var error);

        Assert.Multiple(() =>
        {
            Assert.That(succeeded, Is.True);
            Assert.That(error, Is.Null);
            Assert.That(dialogs.Count, Is.EqualTo(2));
            Assert.That(dialogs.IsOpen, Is.True);
            Assert.That(
                dialogs.Dialogs,
                Is.EqualTo(
                    new[]
                    {
                        new MessageDialogSnapshot(
                            treeDepth: 2,
                            registrationIdentity: 11,
                            "Sense\nEidolon"),
                        new MessageDialogSnapshot(
                            treeDepth: 1,
                            registrationIdentity: 12,
                            "Peek")
                    }));
        });
    }

    [TestCase(false, true)]
    [TestCase(true, false)]
    public void ShouldExcludeDialogsThatAreNotVisibleAndRegistered(
        bool isVisible,
        bool isRegistered)
    {
        var source = new MemoryImageSource();
        WriteEventList(
            source,
            [Entry(0x3000, treeDepth: 0, identity: 1)]);
        WritePaneState(
            source,
            paneAddress: 0x3000,
            ExpectedVtable,
            LiveCookie,
            isVisible,
            isRegistered);
        var session = new MemoryReadSession(
            source,
            MemoryReadLimits.Client32Bit);

        var succeeded = ClientMessageDialogReader.TryRead(
            session,
            new MemoryAddress(DispatcherAddress),
            new MemoryAddress(ExpectedVtable),
            out var dialogs,
            out var error);

        Assert.Multiple(() =>
        {
            Assert.That(succeeded, Is.True);
            Assert.That(error, Is.Null);
            Assert.That(dialogs, Is.EqualTo(MessageDialogsSnapshot.Empty));
            Assert.That(dialogs.Count, Is.Zero);
        });
    }

    [Test]
    public void ShouldRejectRegistrationListThatChangesDuringCapture()
    {
        var source = new MemoryImageSource();
        WriteEventList(
            source,
            [Entry(0x3000, treeDepth: 0, identity: 1)]);
        WritePaneState(
            source,
            paneAddress: 0x3000,
            vtable: 0x12345678,
            LiveCookie,
            isVisible: true,
            isRegistered: true);
        var entryReads = 0;
        source.ReadStarting = (address, _) =>
        {
            if (address.Value != EntriesAddress ||
                ++entryReads != 2)
            {
                return;
            }

            source.Write(
                new MemoryAddress(EntriesAddress),
                Entry(0x3000, treeDepth: 0, identity: 2));
        };
        var session = new MemoryReadSession(
            source,
            MemoryReadLimits.Client32Bit);

        var succeeded = ClientMessageDialogReader.TryRead(
            session,
            new MemoryAddress(DispatcherAddress),
            new MemoryAddress(ExpectedVtable),
            out var dialogs,
            out var error);

        Assert.Multiple(() =>
        {
            Assert.That(succeeded, Is.False);
            Assert.That(dialogs.Count, Is.Zero);
            Assert.That(
                error?.Failure,
                Is.EqualTo(ClientMessageDialogReadFailure.CollectionChanged));
        });
    }

    [Test]
    public void ShouldRejectInvalidRegistrationBounds()
    {
        var source = new MemoryImageSource();
        WriteEventListHeader(
            source,
            entriesAddress: EntriesAddress,
            count: 2,
            capacity: 1);
        var session = new MemoryReadSession(
            source,
            MemoryReadLimits.Client32Bit);

        var succeeded = ClientMessageDialogReader.TryRead(
            session,
            new MemoryAddress(DispatcherAddress),
            new MemoryAddress(ExpectedVtable),
            out var dialogs,
            out var error);

        Assert.Multiple(() =>
        {
            Assert.That(succeeded, Is.False);
            Assert.That(dialogs.Count, Is.Zero);
            Assert.That(
                error?.Failure,
                Is.EqualTo(ClientMessageDialogReadFailure.InvalidValue));
        });
    }

    [Test]
    public void ShouldReadEmptyRegistrationListWithoutDereferencingStorage()
    {
        var source = new MemoryImageSource();
        WriteEventListHeader(
            source,
            entriesAddress: 0,
            count: 0,
            capacity: 0);
        var session = new MemoryReadSession(
            source,
            MemoryReadLimits.Client32Bit);

        var succeeded = ClientMessageDialogReader.TryRead(
            session,
            new MemoryAddress(DispatcherAddress),
            new MemoryAddress(ExpectedVtable),
            out var dialogs,
            out var error);

        Assert.Multiple(() =>
        {
            Assert.That(succeeded, Is.True);
            Assert.That(error, Is.Null);
            Assert.That(dialogs.Count, Is.Zero);
            Assert.That(
                source.Reads.Any(read => read.Address.Value == 0),
                Is.False);
        });
    }

    private static void WriteEventList(
        MemoryImageSource source,
        IReadOnlyList<byte[]> entries)
    {
        WriteEventListHeader(
            source,
            EntriesAddress,
            entries.Count,
            entries.Count);
        source.Write(
            new MemoryAddress(EntriesAddress),
            entries.SelectMany(static entry => entry).ToArray());
    }

    private static void WriteEventListHeader(
        MemoryImageSource source,
        ulong entriesAddress,
        int count,
        int capacity)
    {
        var header = new byte[12];
        BinaryPrimitives.WriteUInt32LittleEndian(
            header,
            checked((uint)entriesAddress));
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(4), count);
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(8), capacity);
        source.Write(
            new MemoryAddress(DispatcherAddress + 0x64),
            header);
    }

    private static byte[] Entry(
        ulong paneAddress,
        uint treeDepth,
        uint identity)
    {
        var entry = new byte[ClientMessageDialogReader.EntrySize];
        BinaryPrimitives.WriteUInt32LittleEndian(
            entry,
            checked((uint)paneAddress));
        BinaryPrimitives.WriteUInt32LittleEndian(entry.AsSpan(4), treeDepth);
        BinaryPrimitives.WriteUInt32LittleEndian(entry.AsSpan(8), identity);
        return entry;
    }

    private static void WriteDialog(
        MemoryImageSource source,
        ulong paneAddress,
        ulong controlsAddress,
        ulong controlArrayAddress,
        ulong contentControlAddress,
        ulong textPaneAddress,
        ulong characterListAddress,
        ulong textAddress,
        string text,
        uint liveCookie = LiveCookie)
    {
        WritePaneState(
            source,
            paneAddress,
            ExpectedVtable,
            liveCookie,
            isVisible: true,
            isRegistered: true);
        source.WriteUInt32(
            new MemoryAddress(paneAddress + 0x594),
            checked((uint)controlsAddress));

        var controlsHeader = new byte[8];
        BinaryPrimitives.WriteInt32LittleEndian(controlsHeader, 2);
        BinaryPrimitives.WriteUInt32LittleEndian(
            controlsHeader.AsSpan(4),
            checked((uint)controlArrayAddress));
        source.Write(
            new MemoryAddress(controlsAddress + 0x14),
            controlsHeader);
        source.WriteUInt32(
            new MemoryAddress(controlArrayAddress + sizeof(uint)),
            checked((uint)contentControlAddress));
        source.WriteUInt32(
            new MemoryAddress(contentControlAddress + 0x19C),
            checked((uint)textPaneAddress));
        source.WriteUInt32(
            new MemoryAddress(textPaneAddress + 0x1BC),
            checked((uint)characterListAddress));

        var bytes = Encoding.ASCII.GetBytes(text);
        var textHeader = new byte[8];
        BinaryPrimitives.WriteInt32LittleEndian(textHeader, bytes.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(
            textHeader.AsSpan(4),
            checked((uint)textAddress));
        source.Write(
            new MemoryAddress(characterListAddress + 0x14),
            textHeader);
        source.Write(new MemoryAddress(textAddress), bytes);
    }

    private static void WritePaneState(
        MemoryImageSource source,
        ulong paneAddress,
        ulong vtable,
        uint liveCookie,
        bool isVisible,
        bool isRegistered)
    {
        var header = new byte[8];
        BinaryPrimitives.WriteUInt32LittleEndian(
            header,
            checked((uint)vtable));
        BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(4), liveCookie);
        source.Write(new MemoryAddress(paneAddress), header);
        source.Write(
            new MemoryAddress(paneAddress + 0x130),
            isVisible ? (byte)1 : (byte)0);
        source.WriteUInt32(
            new MemoryAddress(paneAddress + 0x188),
            isRegistered
                ? ClientMessageDialogReader.RegisteredFlag
                : 0);
    }
}
