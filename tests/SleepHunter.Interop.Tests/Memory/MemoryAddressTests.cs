using SleepHunter.Interop.Memory;

namespace SleepHunter.Interop.Tests.Memory;

public sealed class MemoryAddressTests
{
    [Test]
    public void ShouldApplySignedOffsetsWithoutWrapping()
    {
        var address = new MemoryAddress(0x1000);

        var added = address.TryOffset(0x20, out var higher);
        var subtracted = address.TryOffset(-0x20, out var lower);
        var underflow = address.TryOffset(-0x2000, out _);
        var overflow = new MemoryAddress(ulong.MaxValue)
            .TryOffset(1, out _);

        Assert.Multiple(() =>
        {
            Assert.That(added, Is.True);
            Assert.That(higher.Value, Is.EqualTo(0x1020));
            Assert.That(subtracted, Is.True);
            Assert.That(lower.Value, Is.EqualTo(0x0FE0));
            Assert.That(underflow, Is.False);
            Assert.That(overflow, Is.False);
            Assert.That(address.ToString(), Is.EqualTo("0x1000"));
        });
    }

    [Test]
    public void ShouldValidateTheEntireRequestedRange()
    {
        var range = new MemoryAddressRange(
            new MemoryAddress(0x1000),
            new MemoryAddress(0x10FF));

        Assert.Multiple(() =>
        {
            Assert.That(range.Contains(new MemoryAddress(0x1000)), Is.True);
            Assert.That(
                range.Contains(new MemoryAddress(0x10FC), length: 4),
                Is.True);
            Assert.That(
                range.Contains(new MemoryAddress(0x10FD), length: 4),
                Is.False);
            Assert.That(range.Contains(MemoryAddress.Null), Is.False);
            Assert.That(
                range.Contains(new MemoryAddress(0x1000), length: 0),
                Is.False);
        });
    }

    [Test]
    public void ShouldValidateReadLimitRelationships()
    {
        var range = MemoryAddressRange.Address32Bit;

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = new MemoryReadLimits(
                    PointerWidth.Bit32,
                    range,
                    maximumBlockBytes: 0));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = new MemoryReadLimits(
                    PointerWidth.Bit32,
                    range,
                    maximumBlockBytes: 8,
                    maximumStringBytes: 9));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = new MemoryReadLimits(
                    PointerWidth.Bit32,
                    range,
                    maximumBlockBytes: 8,
                    maximumTotalBytes: 7));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = new MemoryReadLimits(
                    PointerWidth.Bit32,
                    range,
                    maximumReadCount: 0));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = new MemoryReadLimits(
                    PointerWidth.Bit32,
                    range,
                    maximumPointerDepth: 0));
        });
    }
}
