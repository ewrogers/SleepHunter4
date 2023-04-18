using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

using SleepHunter.Common;

namespace SleepHunter.Models
{
    internal enum TargetCoordinateUnits
    {
        None = 0,
        Self,
        Character,
        RelativeTile,
        AbsoluteTile,
        AbsoluteXY,
        RelativeRadius,
        AbsoluteRadius
    }

    internal sealed class SpellTarget : ObservableObject
    {
        private TargetCoordinateUnits unitType;
        private string characterName;
        private Point location = new Point();
        private Point offset = new Point();
        private int innerRadius;
        private int outerRadius;
        private List<Point> radiusPoints;
        private int radiusIndex;

        public TargetCoordinateUnits Units
        {
            get { return unitType; }
            set { SetProperty(ref unitType, value, onChanged: (p) => { RecalculatePoints(); }); }
        }

        public string CharacterName
        {
            get { return characterName; }
            set { SetProperty(ref characterName, value); }
        }

        public Point Location
        {
            get { return location; }
            set { SetProperty(ref location, value, onChanged: (p) => { RecalculatePoints(); }); }
        }

        public Point Offset
        {
            get { return offset; }
            set { SetProperty(ref offset, value); }
        }

        public int InnerRadius
        {
            get { return innerRadius; }
            set { SetProperty(ref innerRadius, value, onChanged: (p) => { RecalculatePoints(); }); }
        }

        public int OuterRadius
        {
            get { return outerRadius; }
            set { SetProperty(ref outerRadius, value, onChanged: (p) => { RecalculatePoints(); }); }
        }

        public IList<Point> RadiusPoints
        {
            get { return radiusPoints; }
        }

        public int RadiusIndex
        {
            get { return radiusIndex; }
            set { SetProperty(ref radiusIndex, value); }
        }

        public SpellTarget()
           : this(TargetCoordinateUnits.None, new Point(), new Point()) { }

        public SpellTarget(TargetCoordinateUnits units, Point location)
           : this(units, location, new Point()) { }

        public SpellTarget(TargetCoordinateUnits units, Point location, Point offset)
        {
            unitType = units;
            this.location = location;
            this.offset = offset;

            RecalculatePoints();
        }

        public void RecalculatePoints()
        {
            if (unitType == TargetCoordinateUnits.RelativeRadius || unitType == TargetCoordinateUnits.AbsoluteRadius)
            {
                radiusPoints = GetRadiusPoints(location, innerRadius, outerRadius).ToList();
                radiusPoints.Sort(ComparePolarAscending);
            }
            else
                radiusPoints = null;

            RaisePropertyChanged("RadiusPoints");
            RadiusIndex = 0;
        }

        public Point GetNextRadiusPoint()
        {
            if (radiusPoints == null || radiusPoints.Count == 0)
                return location;

            if (RadiusIndex >= radiusPoints.Count)
                RadiusIndex = 0;

            var point = radiusPoints[RadiusIndex++];

            if (RadiusIndex >= radiusPoints.Count)
                RadiusIndex = 0;

            return point;
        }

        private int ComparePolarAscending(Point a, Point b)
        {
            return ComparePolar(a, b, isDescending: false);
        }

        private int ComparePolarDescending(Point a, Point b)
        {
            return ComparePolar(a, b, isDescending: true);
        }

        private int ComparePolar(Point a, Point b, bool isDescending = false)
        {
            var polarA = RectToPolar(a);
            var polarB = RectToPolar(b);

            var radiusADist = polarA.X - location.X;
            var radiusBDist = polarB.X - location.X;

            var angleA = (180 * (polarA.Y - location.Y)) / Math.PI;
            var angleB = (180 * (polarB.Y - location.Y)) / Math.PI;

            if (angleA < 0)
                angleA += 360;

            if (angleB < 0)
                angleB += 360;

            if (radiusADist > radiusBDist)
                return isDescending ? -1 : 1;
            else if (radiusADist < radiusBDist)
                return isDescending ? 1 : -1;

            if (angleA > angleB)
                return isDescending ? -1 : 1;
            else if (angleA < angleB)
                return isDescending ? 1 : -1;

            return 0;
        }

        private double Determinant(Point a, Point b)
        {
            var det = (a.X * b.Y - a.Y * b.X);
            return det;
        }

        private Point RectToPolar(Point pt)
        {
            var r = Math.Sqrt(pt.X * pt.X + pt.Y * pt.Y);
            var theta = Math.Atan2(pt.Y, pt.X);

            var polar = new Point(r, theta);
            return polar;
        }

        private Point PolarToRect(Point pt)
        {
            var r = pt.X;
            var theta = pt.Y;

            var x = r * Math.Cos(theta);
            var y = r * Math.Sin(theta);

            var rect = new Point(x, y);
            return rect;
        }

        private IEnumerable<Point> GetRadiusPoints(Point center, int innerRadius, int outerRadius)
        {
            for (int i = 0; i <= outerRadius; i++)
            {
                if (i < innerRadius)
                    continue;

                if (i == 0)
                {
                    yield return center;
                    continue;
                }

                var left = new Point(center.X - i, center.Y);
                var right = new Point(center.X + i, center.Y);
                var up = new Point(center.X, center.Y - i);
                var down = new Point(center.X, center.Y + i);

                yield return up;
                yield return right;
                yield return down;
                yield return left;
            }

            var diagonal = (outerRadius - 1);

            for (int y = 1; y <= diagonal; y++)
            {
                if (y < innerRadius)
                    continue;

                for (int x = 1; x <= diagonal; x++)
                {
                    if (x < innerRadius)
                        continue;

                    var x1 = new Point(center.X + x, center.Y + y);
                    var x2 = new Point(center.X - x, center.Y + y);
                    var y1 = new Point(center.X + x, center.Y - y);
                    var y2 = new Point(center.X - x, center.Y - y);

                    yield return x1;
                    yield return x2;
                    yield return y1;
                    yield return y2;
                }
            }
        }

        private string ToRelativeString(Point pt)
        {
            if (pt.X == 0 && pt.Y == 0)
                return "Self";

            var sb = new StringBuilder();

            if (pt.X > 0)
                sb.AppendFormat("{0} Right", pt.X.ToString());
            else if (pt.X < 0)
                sb.AppendFormat("{0} Left", Math.Abs(pt.X).ToString());

            if (pt.X != 0 && pt.Y != 0)
                sb.Append(", ");

            if (pt.Y > 0)
                sb.AppendFormat("{0} Down", pt.Y.ToString());
            else if (pt.Y < 0)
                sb.AppendFormat("{0} Up", Math.Abs(pt.Y).ToString());

            return sb.ToString();
        }

        public override string ToString()
        {
            switch (unitType)
            {
                case TargetCoordinateUnits.None:
                    return null;

                case TargetCoordinateUnits.Character:
                    return string.Format("{0}", characterName);

                case TargetCoordinateUnits.AbsoluteTile:
                    return string.Format("Tile {0}, {1}", location.X.ToString(), location.Y.ToString());

                case TargetCoordinateUnits.AbsoluteXY:
                    return string.Format("Screen {0}, {1}", location.X.ToString(), location.Y.ToString());

                case TargetCoordinateUnits.RelativeTile:
                    return string.Format("{0}", ToRelativeString(location));

                case TargetCoordinateUnits.Self:
                    return string.Format("Self");

                case TargetCoordinateUnits.RelativeRadius:
                    return string.Format("{0} Tile Radius from {1}",
                       (OuterRadius - InnerRadius + 1).ToString(),
                       ToRelativeString(Location));

                case TargetCoordinateUnits.AbsoluteRadius:
                    return string.Format("{0} Tile Radius from {1}, {2}",
                       (OuterRadius - InnerRadius + 1).ToString(),
                       Location.X.ToString(), Location.Y.ToString());
            }

            return string.Empty;
        }
    }
}
