using System.Text;
using SleepHunter.Interop.Memory;

namespace SleepHunter.Interop.Tests.Memory;

public sealed class MemoryReadSessionTests
{
    private static readonly MemoryAddressRange TestRange = new(
        new MemoryAddress(0x1000),
        new MemoryAddress(0x1FFF));

    [Test]
    public void ShouldReadLittleEndianValuesAndTrackExactMetrics()
    {
        var source = new MemoryImageSource();
        source.Write(
            new MemoryAddress(0x1000),
            0x34,
            0x12,
            0x78,
            0x56,
            0x34,
            0x12);
        var session = CreateSession(source);

        var readShort = session.TryReadUInt16(
            new MemoryAddress(0x1000),
            out var shortValue,
            out var shortError);
        var readInteger = session.TryReadInt32(
            new MemoryAddress(0x1002),
            out var integerValue,
            out var integerError);

        Assert.Multiple(() =>
        {
            Assert.That(readShort, Is.True);
            Assert.That(shortValue, Is.EqualTo(0x1234));
            Assert.That(shortError, Is.Null);
            Assert.That(readInteger, Is.True);
            Assert.That(integerValue, Is.EqualTo(0x12345678));
            Assert.That(integerError, Is.Null);
            Assert.That(
                session.Metrics,
                Is.EqualTo(new MemoryReadMetrics(
                    RequestCount: 2,
                    TransportReadCount: 2,
                    FailedReadCount: 0,
                    RequestedBytes: 6,
                    BytesRead: 6)));
        });
    }

    [Test]
    public void ShouldRejectInvalidAndOverBudgetReadsBeforeTransport()
    {
        var source = new MemoryImageSource();
        source.Write(new MemoryAddress(0x1000), Enumerable.Repeat(
            (byte)1,
            16).ToArray());
        var limits = new MemoryReadLimits(
            PointerWidth.Bit32,
            TestRange,
            maximumBlockBytes: 8,
            maximumStringBytes: 8,
            maximumTotalBytes: 10,
            maximumReadCount: 2);
        var session = new MemoryReadSession(source, limits);
        Span<byte> eight = stackalloc byte[8];
        Span<byte> four = stackalloc byte[4];
        Span<byte> nine = stackalloc byte[9];

        var invalidAddress = session.TryRead(
            MemoryAddress.Null,
            four,
            out var addressError);
        var tooLarge = session.TryRead(
            new MemoryAddress(0x1000),
            nine,
            out var sizeError);
        var first = session.TryRead(
            new MemoryAddress(0x1000),
            eight,
            out _);
        var overBytes = session.TryRead(
            new MemoryAddress(0x1008),
            four,
            out var budgetError);

        Assert.Multiple(() =>
        {
            Assert.That(invalidAddress, Is.False);
            Assert.That(
                addressError?.Failure,
                Is.EqualTo(MemoryReadFailure.InvalidAddress));
            Assert.That(tooLarge, Is.False);
            Assert.That(
                sizeError?.Failure,
                Is.EqualTo(MemoryReadFailure.BlockLimitExceeded));
            Assert.That(first, Is.True);
            Assert.That(overBytes, Is.False);
            Assert.That(
                budgetError?.Failure,
                Is.EqualTo(MemoryReadFailure.ByteBudgetExceeded));
            Assert.That(source.Reads, Has.Count.EqualTo(1));
            Assert.That(session.Metrics.RequestCount, Is.EqualTo(4));
            Assert.That(session.Metrics.FailedReadCount, Is.EqualTo(3));
        });
    }

    [Test]
    public void ShouldReportPartialAndTransportFailures()
    {
        var source = new MemoryImageSource();
        source.Write(new MemoryAddress(0x1000), 1, 2);
        var session = CreateSession(source);
        Span<byte> buffer = stackalloc byte[4];

        var partial = session.TryRead(
            new MemoryAddress(0x1000),
            buffer,
            out var partialError);
        var missing = session.TryRead(
            new MemoryAddress(0x1100),
            buffer,
            out var missingError);

        Assert.Multiple(() =>
        {
            Assert.That(partial, Is.False);
            Assert.That(
                partialError,
                Is.EqualTo(new MemoryReadError(
                    MemoryReadFailure.PartialRead,
                    new MemoryAddress(0x1000),
                    RequestedBytes: 4,
                    BytesRead: 2,
                    NativeErrorCode: 299)));
            Assert.That(missing, Is.False);
            Assert.That(
                missingError?.Failure,
                Is.EqualTo(MemoryReadFailure.TransportFailure));
            Assert.That(session.Metrics.BytesRead, Is.EqualTo(2));
        });
    }

    [Test]
    public void ShouldEnforceTransportReadCountBudget()
    {
        var source = new MemoryImageSource();
        source.Write(new MemoryAddress(0x1000), 1, 2);
        var session = new MemoryReadSession(
            source,
            new MemoryReadLimits(
                PointerWidth.Bit32,
                TestRange,
                maximumBlockBytes: 8,
                maximumStringBytes: 8,
                maximumTotalBytes: 16,
                maximumReadCount: 1));
        Span<byte> firstBuffer = stackalloc byte[1];
        Span<byte> secondBuffer = stackalloc byte[1];

        var first = session.TryRead(
            new MemoryAddress(0x1000),
            firstBuffer,
            out _);
        var second = session.TryRead(
            new MemoryAddress(0x1001),
            secondBuffer,
            out var error);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.True);
            Assert.That(second, Is.False);
            Assert.That(
                error?.Failure,
                Is.EqualTo(MemoryReadFailure.ReadBudgetExceeded));
            Assert.That(source.Reads, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void ShouldReadBoundedStringsWithExplicitTerminationPolicy()
    {
        var source = new MemoryImageSource();
        source.Write(
            new MemoryAddress(0x1000),
            (byte)'A',
            (byte)'l',
            (byte)'t',
            0,
            (byte)'x');
        source.Write(
            new MemoryAddress(0x1010),
            (byte)'T',
            (byte)'e',
            (byte)'s',
            (byte)'t');
        source.Write(new MemoryAddress(0x1020), 0xFF, 0, 0);
        var session = CreateSession(source);
        var strictUtf8 = new UTF8Encoding(
            encoderShouldEmitUTF8Identifier: false,
            throwOnInvalidBytes: true);

        var terminated = session.TryReadString(
            new MemoryAddress(0x1000),
            maximumBytes: 5,
            Encoding.ASCII,
            out var first,
            out _,
            requireTerminator: true);
        var fixedLength = session.TryReadString(
            new MemoryAddress(0x1010),
            maximumBytes: 4,
            Encoding.ASCII,
            out var second,
            out _);
        var missing = session.TryReadString(
            new MemoryAddress(0x1010),
            maximumBytes: 4,
            Encoding.ASCII,
            out _,
            out var missingError,
            requireTerminator: true);
        var invalid = session.TryReadString(
            new MemoryAddress(0x1020),
            maximumBytes: 3,
            strictUtf8,
            out _,
            out var encodingError);
        var overLimit = session.TryReadString(
            new MemoryAddress(0x1000),
            maximumBytes: 33,
            Encoding.ASCII,
            out _,
            out var limitError);

        Assert.Multiple(() =>
        {
            Assert.That(terminated, Is.True);
            Assert.That(first, Is.EqualTo("Alt"));
            Assert.That(fixedLength, Is.True);
            Assert.That(second, Is.EqualTo("Test"));
            Assert.That(missing, Is.False);
            Assert.That(
                missingError?.Failure,
                Is.EqualTo(MemoryReadFailure.MissingTerminator));
            Assert.That(invalid, Is.False);
            Assert.That(
                encodingError?.Failure,
                Is.EqualTo(MemoryReadFailure.InvalidEncoding));
            Assert.That(overLimit, Is.False);
            Assert.That(
                limitError?.Failure,
                Is.EqualTo(MemoryReadFailure.StringLimitExceeded));
        });
    }

    private static MemoryReadSession CreateSession(
        IProcessMemorySource source) =>
        new(
            source,
            new MemoryReadLimits(
                PointerWidth.Bit32,
                TestRange,
                maximumBlockBytes: 64,
                maximumStringBytes: 32,
                maximumTotalBytes: 1024,
                maximumReadCount: 32));
}
