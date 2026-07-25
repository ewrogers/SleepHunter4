using SleepHunter.Runtime.Automation.Spells;

namespace SleepHunter.Runtime.Tests.Automation.Spells;

public sealed class SpellCastTimingPolicyTests
{
    [Test]
    public void ShouldCalculateZeroSingleAndMultiLineDurations()
    {
        var policy = new SpellCastTimingPolicy(
            TimeSpan.FromMilliseconds(200),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromMilliseconds(750),
            TimeSpan.FromMilliseconds(100));

        Assert.Multiple(() =>
        {
            Assert.That(
                policy.CalculateDuration(castLines: 0),
                Is.EqualTo(TimeSpan.FromMilliseconds(300)));
            Assert.That(
                policy.CalculateDuration(castLines: 1),
                Is.EqualTo(TimeSpan.FromMilliseconds(1100)));
            Assert.That(
                policy.CalculateDuration(castLines: 4),
                Is.EqualTo(TimeSpan.FromMilliseconds(3100)));
        });
    }

    [Test]
    public void ShouldRejectInvalidTimingValues()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = new SpellCastTimingPolicy(
                    TimeSpan.FromTicks(-1),
                    TimeSpan.Zero,
                    TimeSpan.Zero,
                    TimeSpan.FromTicks(1)));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = new SpellCastTimingPolicy(
                    TimeSpan.Zero,
                    TimeSpan.FromTicks(-1),
                    TimeSpan.Zero,
                    TimeSpan.FromTicks(1)));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = new SpellCastTimingPolicy(
                    TimeSpan.Zero,
                    TimeSpan.Zero,
                    TimeSpan.FromTicks(-1),
                    TimeSpan.FromTicks(1)));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = new SpellCastTimingPolicy(
                    TimeSpan.Zero,
                    TimeSpan.Zero,
                    TimeSpan.Zero,
                    TimeSpan.Zero));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => SpellCastTimingPolicy.Default.CalculateDuration(-1));
            Assert.Throws<OverflowException>(
                () => new SpellCastTimingPolicy(
                        TimeSpan.Zero,
                        TimeSpan.Zero,
                        TimeSpan.MaxValue,
                        TimeSpan.FromTicks(1))
                    .CalculateDuration(2));
        });
    }
}
