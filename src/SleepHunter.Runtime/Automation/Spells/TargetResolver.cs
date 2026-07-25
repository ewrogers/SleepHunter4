namespace SleepHunter.Runtime.Automation.Spells;

public static class TargetResolver
{
    public static TargetResolution Resolve(
        SpellTarget target,
        int cursor = 0)
    {
        ArgumentNullException.ThrowIfNull(target);

        if (cursor < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(cursor),
                cursor,
                "The target cursor cannot be negative.");
        }

        if (!target.IsArea)
        {
            return new TargetResolution(
                target,
                SelectedIndex: 0,
                NextIndex: 0,
                PointCount: 1);
        }

        var points = CreatePoints(target);
        var selectedIndex = cursor % points.Count;
        var point = points[selectedIndex];
        var resolved = target.Kind switch
        {
            SpellTargetKind.RelativeArea =>
                SpellTarget.RelativeTile(
                    point.X,
                    point.Y,
                    target.Offset),
            SpellTargetKind.AbsoluteArea =>
                SpellTarget.AbsoluteTile(
                    point.X,
                    point.Y,
                    target.Offset),
            _ => throw new InvalidOperationException(
                "Only area targets can be resolved.")
        };
        return new TargetResolution(
            resolved,
            selectedIndex,
            (selectedIndex + 1) % points.Count,
            points.Count);
    }

    private static List<TargetPoint> CreatePoints(SpellTarget target)
    {
        var centerX = target.X!.Value;
        var centerY = target.Y!.Value;
        var innerRadius = target.InnerRadius!.Value;
        var outerRadius = target.OuterRadius!.Value;
        var innerSquared = checked(innerRadius * innerRadius);
        var outerSquared = checked(outerRadius * outerRadius);
        var points = new List<TargetPoint>();

        for (var y = -outerRadius; y <= outerRadius; y++)
        {
            for (var x = -outerRadius; x <= outerRadius; x++)
            {
                var distanceSquared = checked((x * x) + (y * y));
                if (distanceSquared < innerSquared ||
                    distanceSquared > outerSquared)
                {
                    continue;
                }

                var resolvedX = (long)centerX + x;
                var resolvedY = (long)centerY + y;
                if (resolvedX < int.MinValue ||
                    resolvedX > int.MaxValue ||
                    resolvedY < int.MinValue ||
                    resolvedY > int.MaxValue ||
                    target.Kind == SpellTargetKind.AbsoluteArea &&
                    (resolvedX < 0 || resolvedY < 0))
                {
                    continue;
                }

                points.Add(new TargetPoint(
                    (int)resolvedX,
                    (int)resolvedY,
                    distanceSquared,
                    GetClockwiseAngle(x, y),
                    x,
                    y));
            }
        }

        if (points.Count == 0)
        {
            throw new InvalidOperationException(
                "The target area does not contain a valid point.");
        }

        points.Sort(TargetPointComparer.Instance);
        return points;
    }

    private static double GetClockwiseAngle(int x, int y)
    {
        var angle = Math.Atan2(x, -y);
        return angle < 0
            ? angle + (Math.PI * 2)
            : angle;
    }

    private sealed record TargetPoint(
        int X,
        int Y,
        int DistanceSquared,
        double Angle,
        int OffsetX,
        int OffsetY);

    private sealed class TargetPointComparer : IComparer<TargetPoint>
    {
        public static TargetPointComparer Instance { get; } = new();

        public int Compare(TargetPoint? left, TargetPoint? right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left is null)
            {
                return -1;
            }

            if (right is null)
            {
                return 1;
            }

            var result = left.DistanceSquared.CompareTo(right.DistanceSquared);
            if (result != 0)
            {
                return result;
            }

            result = left.Angle.CompareTo(right.Angle);
            if (result != 0)
            {
                return result;
            }

            result = left.OffsetX.CompareTo(right.OffsetX);
            return result != 0
                ? result
                : left.OffsetY.CompareTo(right.OffsetY);
        }
    }
}
