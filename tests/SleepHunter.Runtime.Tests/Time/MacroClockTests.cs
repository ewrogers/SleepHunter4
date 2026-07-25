using SleepHunter.Runtime.Tests.Scenarios;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Tests.Time;

public sealed class MacroClockTests
{
    [Test]
    public void ShouldMeasureElapsedTimeThroughInjectedTimeProvider()
    {
        var timeProvider = new ManualTimeProvider();
        var clock = new MacroClock(timeProvider);

        timeProvider.Advance(TimeSpan.FromMilliseconds(125));
        var first = clock.GetCurrentTimestamp();
        timeProvider.Advance(TimeSpan.FromSeconds(2));
        var second = clock.GetCurrentTimestamp();

        Assert.Multiple(() =>
        {
            Assert.That(first.Elapsed, Is.EqualTo(TimeSpan.FromMilliseconds(125)));
            Assert.That(second.Elapsed, Is.EqualTo(TimeSpan.FromMilliseconds(2125)));
        });
    }

    [Test]
    public void ShouldRejectBackwardVirtualTime()
    {
        var timeProvider = new ManualTimeProvider();

        Assert.That(
            () => timeProvider.Advance(TimeSpan.FromTicks(-1)),
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }
}
