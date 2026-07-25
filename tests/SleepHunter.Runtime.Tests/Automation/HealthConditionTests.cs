using SleepHunter.Runtime.Automation;
using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Runtime.Tests.Automation;

public sealed class HealthConditionTests
{
    [Test]
    public void ShouldUseExclusiveMinimumAndInclusiveMaximum()
    {
        var aboveNinety = new HealthCondition(
            minimumPercentExclusive: 90);
        var atMostTwo = new HealthCondition(
            maximumPercentInclusive: 2);

        Assert.Multiple(() =>
        {
            Assert.That(
                aboveNinety.IsSatisfiedBy(Vitals(90, 100)),
                Is.False);
            Assert.That(
                aboveNinety.IsSatisfiedBy(Vitals(91, 100)),
                Is.True);
            Assert.That(
                atMostTwo.IsSatisfiedBy(Vitals(2, 100)),
                Is.True);
            Assert.That(
                atMostTwo.IsSatisfiedBy(Vitals(3, 100)),
                Is.False);
            Assert.That(
                HealthCondition.Any.IsSatisfiedBy(Vitals(0, 100)),
                Is.True);
            Assert.That(
                atMostTwo.IsSatisfiedBy(Vitals(0, 0)),
                Is.False);
            Assert.That(
                HealthCondition.Any.IsSatisfiedBy(Vitals(0, 0)),
                Is.True);
        });
    }

    [Test]
    public void ShouldRejectInvalidHealthRanges()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = new HealthCondition(
                    minimumPercentExclusive: -1));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = new HealthCondition(
                    maximumPercentInclusive: 101));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = new HealthCondition(double.NaN));
            Assert.Throws<ArgumentException>(
                () => _ = new HealthCondition(50, 50));
            Assert.Throws<ArgumentException>(
                () => _ = new HealthCondition(51, 50));
        });
    }

    private static VitalsSnapshot Vitals(int health, int maximumHealth) =>
        new(
            health,
            maximumHealth,
            currentMana: 0,
            maximumMana: 0);
}
