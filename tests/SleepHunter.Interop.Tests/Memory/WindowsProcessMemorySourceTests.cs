using Microsoft.Win32.SafeHandles;
using SleepHunter.Interop.Memory;

namespace SleepHunter.Interop.Tests.Memory;

public sealed class WindowsProcessMemorySourceTests
{
    [Test]
    public void ShouldRejectInvalidOrClosedHandles()
    {
        using var invalid = new SafeProcessHandle(
            nint.Zero,
            ownsHandle: false);
        var closed = new SafeProcessHandle(
            new nint(1),
            ownsHandle: false);
        closed.Dispose();

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(
                () => _ = new WindowsProcessMemorySource(invalid));
            Assert.Throws<ArgumentException>(
                () => _ = new WindowsProcessMemorySource(closed));
        });
    }

    [Test]
    public void ShouldLeaveHandleOwnershipWithCaller()
    {
        using var handle = new SafeProcessHandle(
            new nint(1),
            ownsHandle: false);
        var source = new WindowsProcessMemorySource(handle);

        var result = source.Read(
            new MemoryAddress(1),
            Span<byte>.Empty);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(MemorySourceReadResult.Failed()));
            Assert.That(handle.IsClosed, Is.False);
        });
    }

    [Test]
    public void ShouldFailSafelyWhenCallerClosesHandle()
    {
        var handle = new SafeProcessHandle(
            new nint(1),
            ownsHandle: false);
        var source = new WindowsProcessMemorySource(handle);
        handle.Dispose();
        Span<byte> buffer = stackalloc byte[1];

        var result = source.Read(new MemoryAddress(1), buffer);

        Assert.That(
            result,
            Is.EqualTo(MemorySourceReadResult.Failed(
                nativeErrorCode: 6)));
    }
}
