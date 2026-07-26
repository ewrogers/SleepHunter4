namespace SleepHunter.Runtime.Tests.Automation.Spells;

using SleepHunter.Runtime.Automation.Spells;

public sealed class TargetResolverTests
{
    [Test]
    public void ShouldResolveCircularAreaByRadiusThenClockwiseFromUp()
    {
        var target = SpellTarget.RelativeArea(
            10,
            20,
            innerRadius: 0,
            outerRadius: 1,
            new TargetOffset(3, -4));

        var resolved = Enumerable.Range(0, 5)
            .Select(cursor => TargetResolver.Resolve(target, cursor))
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(
                resolved.Select(result => (result.Target.X, result.Target.Y)),
                Is.EqualTo(new[]
                {
                    (10, 20),
                    (10, 19),
                    (11, 20),
                    (10, 21),
                    (9, 20)
                }));
            Assert.That(
                resolved.All(result =>
                    result.Target.Kind == SpellTargetKind.RelativeTile),
                Is.True);
            Assert.That(
                resolved.All(result =>
                    result.Target.Offset == new TargetOffset(3, -4)),
                Is.True);
            Assert.That(resolved[4].NextIndex, Is.Zero);
            Assert.That(resolved[0].PointCount, Is.EqualTo(5));
        });
    }

    [Test]
    public void ShouldUseEuclideanCircleAndHonorInnerRadius()
    {
        var target = SpellTarget.RelativeArea(
            0,
            0,
            innerRadius: 1,
            outerRadius: 2);
        var points = Enumerable.Range(0, 12)
            .Select(cursor => TargetResolver.Resolve(target, cursor).Target)
            .Select(point => (point.X!.Value, point.Y!.Value))
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(points, Has.Length.EqualTo(12));
            Assert.That(points, Does.Contain((0, -2)));
            Assert.That(points, Does.Contain((1, -1)));
            Assert.That(points, Does.Not.Contain((0, 0)));
            Assert.That(points, Does.Not.Contain((2, 2)));
        });
    }

    [Test]
    public void ShouldFilterInvalidAbsolutePointsAndWrapLargeCursor()
    {
        var target = SpellTarget.AbsoluteArea(
            0,
            0,
            innerRadius: 0,
            outerRadius: 1);

        var first = TargetResolver.Resolve(target);
        var wrapped = TargetResolver.Resolve(target, cursor: 4);

        Assert.Multiple(() =>
        {
            Assert.That(first.PointCount, Is.EqualTo(3));
            Assert.That(
                (first.Target.X, first.Target.Y),
                Is.EqualTo((0, 0)));
            Assert.That(
                (wrapped.Target.X, wrapped.Target.Y),
                Is.EqualTo((1, 0)));
            Assert.That(wrapped.SelectedIndex, Is.EqualTo(1));
        });
    }

    [Test]
    public void ShouldReturnNonAreaTargetWithoutRotation()
    {
        var target = SpellTarget.Character("Alt").WithOffset(2, 3);

        var result = TargetResolver.Resolve(target, cursor: 99);

        Assert.Multiple(() =>
        {
            Assert.That(result.Target, Is.SameAs(target));
            Assert.That(result.SelectedIndex, Is.Zero);
            Assert.That(result.NextIndex, Is.Zero);
            Assert.That(result.PointCount, Is.EqualTo(1));
        });
    }
}
