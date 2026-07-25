using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Runtime.Tests.Snapshots;

public sealed class MapLocationSnapshotTests
{
    [Test]
    public void ShouldRequireSameMapAndBoundBothAxes()
    {
        var source = new MapLocationSnapshot(1, "map", 50, 50);

        Assert.Multiple(() =>
        {
            Assert.That(
                source.IsWithinRange(
                    new MapLocationSnapshot(1, "map", 60, 60)),
                Is.True);
            Assert.That(
                source.IsWithinRange(
                    new MapLocationSnapshot(1, "map", 61, 50)),
                Is.False);
            Assert.That(
                source.IsWithinRange(
                    new MapLocationSnapshot(1, "map", 50, 61)),
                Is.False);
            Assert.That(
                source.IsWithinRange(
                    new MapLocationSnapshot(2, "map", 50, 50)),
                Is.False);
            Assert.That(
                source.IsWithinRange(
                    new MapLocationSnapshot(1, "Map", 50, 50)),
                Is.False);
        });
    }

    [Test]
    public void ShouldRejectUnknownMapAndInvalidCoordinates()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                () => new MapLocationSnapshot(0, "map", 0, 0),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => new MapLocationSnapshot(1, " ", 0, 0),
                Throws.ArgumentException);
            Assert.That(
                () => new MapLocationSnapshot(1, "map", -1, 0),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => new MapLocationSnapshot(1, "map", 0, -1),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }
}
