using SleepHunter.Interop.Mappings;
using SleepHunter.Interop.Memory;
using SleepHunter.Interop.Tests.Memory;

namespace SleepHunter.Interop.Tests.Mappings;

public sealed class PointerChainResolverTests
{
    [Test]
    public void ShouldResolveSigned32BitPointerOffsets()
    {
        var source = new MemoryImageSource();
        source.WriteUInt32(new MemoryAddress(0x1000), 0x2020);
        source.WriteUInt32(new MemoryAddress(0x2000), 0x3000);
        var chain = new PointerChain(
            new MemoryAddress(0x1000),
            [
                new PointerOffset(-0x20),
                new PointerOffset(0x40)
            ]);
        var session = Session(source, maximumDepth: 2);

        var success = PointerChainResolver.TryResolve(
            chain,
            session,
            out var address,
            out var error);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(address, Is.EqualTo(new MemoryAddress(0x3040)));
            Assert.That(error, Is.Null);
            Assert.That(
                source.Reads.Select(read => read.Address.Value),
                Is.EqualTo(new ulong[] { 0x1000, 0x2000 }));
        });
    }

    [Test]
    public void ShouldResolve64BitPointers()
    {
        var source = new MemoryImageSource();
        source.WriteUInt64(
            new MemoryAddress(0x1000),
            0x0000000100002000);
        var limits = new MemoryReadLimits(
            PointerWidth.Bit64,
            new MemoryAddressRange(
                new MemoryAddress(1),
                new MemoryAddress(0x00000001FFFFFFFF)),
            maximumBlockBytes: 64,
            maximumStringBytes: 32,
            maximumTotalBytes: 1024,
            maximumReadCount: 32);
        var session = new MemoryReadSession(source, limits);
        var chain = new PointerChain(
            new MemoryAddress(0x1000),
            [new PointerOffset(0x30)]);

        var success = PointerChainResolver.TryResolve(
            chain,
            session,
            out var address,
            out var error);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(
                address,
                Is.EqualTo(new MemoryAddress(0x0000000100002030)));
            Assert.That(error, Is.Null);
        });
    }

    [Test]
    public void ShouldRejectNullPointersAndExcessiveDepth()
    {
        var source = new MemoryImageSource();
        source.WriteUInt32(new MemoryAddress(0x1000), 0);
        var session = Session(source, maximumDepth: 1);
        var nullChain = new PointerChain(
            new MemoryAddress(0x1000),
            [new PointerOffset(0)]);
        var deepChain = new PointerChain(
            new MemoryAddress(0x1000),
            [new PointerOffset(0), new PointerOffset(0)]);

        var nullResult = PointerChainResolver.TryResolve(
            nullChain,
            session,
            out _,
            out var nullError);
        var deepResult = PointerChainResolver.TryResolve(
            deepChain,
            session,
            out _,
            out var depthError);

        Assert.Multiple(() =>
        {
            Assert.That(nullResult, Is.False);
            Assert.That(
                nullError?.Failure,
                Is.EqualTo(MemoryReadFailure.NullPointer));
            Assert.That(deepResult, Is.False);
            Assert.That(
                depthError?.Failure,
                Is.EqualTo(MemoryReadFailure.PointerDepthExceeded));
            Assert.That(source.Reads, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void ShouldRejectOutOfRangeResolvedAddress()
    {
        var source = new MemoryImageSource();
        source.WriteUInt32(new MemoryAddress(0x1000), 0x3FF0);
        var chain = new PointerChain(
            new MemoryAddress(0x1000),
            [new PointerOffset(0x20)]);
        var session = Session(source, maximumDepth: 1);

        var success = PointerChainResolver.TryResolve(
            chain,
            session,
            out _,
            out var error);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.False);
            Assert.That(
                error?.Failure,
                Is.EqualTo(MemoryReadFailure.InvalidAddress));
        });
    }

    private static MemoryReadSession Session(
        IProcessMemorySource source,
        int maximumDepth) =>
        new(
            source,
            new MemoryReadLimits(
                PointerWidth.Bit32,
                new MemoryAddressRange(
                    new MemoryAddress(0x1000),
                    new MemoryAddress(0x3FFF)),
                maximumBlockBytes: 64,
                maximumStringBytes: 32,
                maximumTotalBytes: 1024,
                maximumReadCount: 32,
                maximumPointerDepth: maximumDepth));
}
