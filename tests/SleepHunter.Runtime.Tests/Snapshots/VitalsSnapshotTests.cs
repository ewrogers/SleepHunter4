using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Runtime.Tests.Snapshots;

public sealed class VitalsSnapshotTests
{
    [Test]
    public void ShouldCalculateBoundedResourcePercentages()
    {
        var normal = new VitalsSnapshot(
            currentHealth: 50,
            maximumHealth: 200,
            currentMana: 75,
            maximumMana: 100);
        var overMaximum = new VitalsSnapshot(
            currentHealth: 300,
            maximumHealth: 200,
            currentMana: 125,
            maximumMana: 100);
        var unknownMaximum = new VitalsSnapshot(
            currentHealth: 10,
            maximumHealth: 0,
            currentMana: 10,
            maximumMana: 0);

        Assert.Multiple(() =>
        {
            Assert.That(normal.HealthPercent, Is.EqualTo(25));
            Assert.That(normal.ManaPercent, Is.EqualTo(75));
            Assert.That(overMaximum.HealthPercent, Is.EqualTo(100));
            Assert.That(overMaximum.ManaPercent, Is.EqualTo(100));
            Assert.That(unknownMaximum.HealthPercent, Is.Zero);
            Assert.That(unknownMaximum.ManaPercent, Is.Zero);
        });
    }

    [Test]
    public void ShouldRejectNegativeResourceValues()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = new VitalsSnapshot(-1, 1, 1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = new VitalsSnapshot(1, -1, 1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = new VitalsSnapshot(1, 1, -1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = new VitalsSnapshot(1, 1, 1, -1));
        });
    }
}
