using System.Collections.Immutable;
using System.Text;
using SleepHunter.Interop.Mappings;
using SleepHunter.Interop.Memory;
using SleepHunter.Interop.Tests.Memory;

namespace SleepHunter.Interop.Tests.Mappings;

public sealed class MappedMemoryReaderTests
{
    [Test]
    public void ShouldReadTypedStaticAndDynamicValues()
    {
        var source = new MemoryImageSource();
        source.Write(new MemoryAddress(0x1000), 0x2A);
        source.WriteUInt32(new MemoryAddress(0x1100), 0x2000);
        source.WriteUInt32(new MemoryAddress(0x2020), 0x3000);
        source.Write(new MemoryAddress(0x2FF0), 0x78, 0x56, 0x34, 0x12);
        var reader = Reader(
            source,
            new MemoryVariableDefinition(
                "StaticByte",
                new PointerChain(new MemoryAddress(0x1000)),
                MemoryValueKind.Byte),
            new MemoryVariableDefinition(
                "DynamicValue",
                new PointerChain(
                    new MemoryAddress(0x1100),
                    ImmutableArray.Create(
                        new PointerOffset(0x20),
                        new PointerOffset(-0x10))),
                MemoryValueKind.Unsigned32));

        var addressSuccess = reader.TryResolveAddress(
            "DynamicValue",
            out var address,
            out var addressError);
        var byteSuccess = reader.TryReadByte(
            "staticbyte",
            out var byteValue,
            out var byteError);
        var valueSuccess = reader.TryReadUInt32(
            "DYNAMICVALUE",
            out var value,
            out var valueError);

        Assert.Multiple(() =>
        {
            Assert.That(addressSuccess, Is.True);
            Assert.That(address, Is.EqualTo(new MemoryAddress(0x2FF0)));
            Assert.That(addressError, Is.Null);
            Assert.That(byteSuccess, Is.True);
            Assert.That(byteValue, Is.EqualTo(0x2A));
            Assert.That(byteError, Is.Null);
            Assert.That(valueSuccess, Is.True);
            Assert.That(value, Is.EqualTo(0x12345678));
            Assert.That(valueError, Is.Null);
        });
    }

    [Test]
    public void ShouldReadBoundedTextAndBinaryValues()
    {
        var source = new MemoryImageSource();
        source.Write(
            new MemoryAddress(0x1000),
            Encoding.ASCII.GetBytes("Aislinn\0........"));
        source.Write(new MemoryAddress(0x2000), 1, 2, 3, 4);
        var reader = Reader(
            source,
            new MemoryVariableDefinition(
                "Name",
                new PointerChain(new MemoryAddress(0x1000)),
                MemoryValueKind.Text,
                maximumLength: 16),
            new MemoryVariableDefinition(
                "Bytes",
                new PointerChain(new MemoryAddress(0x2000)),
                MemoryValueKind.Binary,
                recordSize: 4,
                capacity: 1));
        var bytes = new byte[4];

        var textSuccess = reader.TryReadText(
            "Name",
            Encoding.ASCII,
            out var text,
            out var textError,
            requireTerminator: true);
        var bytesSuccess = reader.TryReadBytes(
            "Bytes",
            bytes,
            out var bytesError);

        Assert.Multiple(() =>
        {
            Assert.That(textSuccess, Is.True);
            Assert.That(text, Is.EqualTo("Aislinn"));
            Assert.That(textError, Is.Null);
            Assert.That(bytesSuccess, Is.True);
            Assert.That(bytes, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
            Assert.That(bytesError, Is.Null);
        });
    }

    [Test]
    public void ShouldReportMappingFailuresWithoutReadingMemory()
    {
        var source = new MemoryImageSource();
        var reader = Reader(
            source,
            new MemoryVariableDefinition(
                "Number",
                new PointerChain(new MemoryAddress(0x1000)),
                MemoryValueKind.Unsigned32),
            new MemoryVariableDefinition(
                "Search",
                new PointerChain(
                    new MemoryAddress(0x2000),
                    ImmutableArray.Create(new PointerOffset(0x10))),
                MemoryValueKind.Binary,
                recordSize: 4,
                search: new MemoryAddressSearch(new PointerOffset(0x20))));
        Span<byte> bytes = stackalloc byte[4];

        var missing = reader.TryReadByte(
            "Missing",
            out _,
            out var missingError);
        var mismatch = reader.TryReadByte(
            "Number",
            out _,
            out var mismatchError);
        var search = reader.TryReadBytes(
            "Search",
            bytes,
            out var searchError);

        Assert.Multiple(() =>
        {
            Assert.That(missing, Is.False);
            Assert.That(
                missingError?.Failure,
                Is.EqualTo(MappedMemoryReadFailure.VariableNotFound));
            Assert.That(mismatch, Is.False);
            Assert.That(
                mismatchError?.Failure,
                Is.EqualTo(MappedMemoryReadFailure.ValueKindMismatch));
            Assert.That(mismatchError?.ActualKind, Is.EqualTo(MemoryValueKind.Unsigned32));
            Assert.That(search, Is.False);
            Assert.That(
                searchError?.Failure,
                Is.EqualTo(MappedMemoryReadFailure.SearchResolutionRequired));
            Assert.That(source.Reads, Is.Empty);
        });
    }

    [Test]
    public void ShouldPreserveAddressAndTransportFailures()
    {
        var source = new MemoryImageSource();
        source.WriteUInt32(new MemoryAddress(0x1000), 0);
        var reader = Reader(
            source,
            new MemoryVariableDefinition(
                "NullChain",
                new PointerChain(
                    new MemoryAddress(0x1000),
                    ImmutableArray.Create(new PointerOffset(0))),
                MemoryValueKind.Byte),
            new MemoryVariableDefinition(
                "MissingBytes",
                new PointerChain(new MemoryAddress(0x2000)),
                MemoryValueKind.Unsigned32));

        var addressSuccess = reader.TryReadByte(
            "NullChain",
            out _,
            out var addressError);
        var valueSuccess = reader.TryReadUInt32(
            "MissingBytes",
            out _,
            out var valueError);

        Assert.Multiple(() =>
        {
            Assert.That(addressSuccess, Is.False);
            Assert.That(
                addressError?.Failure,
                Is.EqualTo(MappedMemoryReadFailure.AddressResolutionFailed));
            Assert.That(
                addressError?.MemoryError?.Failure,
                Is.EqualTo(MemoryReadFailure.NullPointer));
            Assert.That(valueSuccess, Is.False);
            Assert.That(
                valueError?.Failure,
                Is.EqualTo(MappedMemoryReadFailure.ValueReadFailed));
            Assert.That(
                valueError?.MemoryError?.Failure,
                Is.EqualTo(MemoryReadFailure.TransportFailure));
        });
    }

    [Test]
    public void ShouldRejectMismatchedPointerWidths()
    {
        var map = new ClientMemoryMap(
            "Version",
            PointerWidth.Bit64,
            []);
        var session = new MemoryReadSession(
            new MemoryImageSource(),
            MemoryReadLimits.Client32Bit);

        Assert.Throws<ArgumentException>(
            () => _ = new MappedMemoryReader(map, session));
    }

    private static MappedMemoryReader Reader(
        MemoryImageSource source,
        params MemoryVariableDefinition[] variables)
    {
        var map = new ClientMemoryMap(
            "Version",
            PointerWidth.Bit32,
            variables);
        var session = new MemoryReadSession(
            source,
            MemoryReadLimits.Client32Bit);
        return new MappedMemoryReader(map, session);
    }
}
