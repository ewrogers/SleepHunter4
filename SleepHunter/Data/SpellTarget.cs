using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SleepHunter.Data
{
   public enum TargetCoordinateUnits
   {
      None = 0,
      Self,
      Character,
      RelativeTile,
      RelativeXY,
      AbsoluteTile,
      AbsoluteXY,
      RelativeRadius,
      AbsoluteRadius
   }

   public sealed class SpellTarget : NotifyObject
   {
      TargetCoordinateUnits unitType;
      string characterName;
      Point location = new Point();
      Point offset = new Point();
      int innerRadius;
      int outerRadius;
      List<Point> radiusPoints;
      int radiusIndex;

      public TargetCoordinateUnits Units
      {
         get { return unitType; }
         set { SetProperty(ref unitType, value, "Units", onChanged: (p) => { RecalculatePoints(); }); }
      }

      public string CharacterName
      {
         get { return characterName; }
         set { SetProperty(ref characterName, value, "CharacterName"); }
      }

      public Point Location
      {
         get { return location; }
         set { SetProperty(ref location, value, "Location", onChanged: (p) => { RecalculatePoints(); }); }
      }

      public Point Offset
      {
         get { return offset; }
         set { SetProperty(ref offset, value, "Offset"); }
      }

      public int InnerRadius
      {
         get { return innerRadius; }
         set { SetProperty(ref innerRadius, value, "InnerRadius", onChanged: (p) => { RecalculatePoints(); }); }
      }

      public int OuterRadius
      {
         get { return outerRadius; }
         set { SetProperty(ref outerRadius, value, "OuterRadius", onChanged: (p) => { RecalculatePoints(); }); }
      }

      public IList<Point> RadiusPoints
      {
         get { return radiusPoints; }
      }

      public int RadiusIndex
      {
         get { return radiusIndex; }
         set { SetProperty(ref radiusIndex, value, "RadiusIndex"); }
      }

      public SpellTarget()
         : this(TargetCoordinateUnits.None, new Point(), new Point()) { }

      public SpellTarget(TargetCoordinateUnits units, Point location)
         : this(units, location, new Point()) { }

      public SpellTarget(TargetCoordinateUnits units, Point location, Point offset)
      {
         this.unitType = units;
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
         else radiusPoints = null;

         OnPropertyChanged("RadiusPoints");
         this.RadiusIndex = 0;
      }

      public Point GetNextRadiusPoint()
      {
         if (radiusPoints == null || radiusPoints.Count == 0)
            return location;

         if (this.RadiusIndex >= radiusPoints.Count)
            this.RadiusIndex = 0;

         var point = radiusPoints[this.RadiusIndex++];

         if (this.RadiusIndex >= radiusPoints.Count)
            this.RadiusIndex = 0;

         return point;
      }

      int ComparePolarAscending(Point a, Point b)
      {
         return ComparePolar(a, b, isDescending: false);
      }

      int ComparePolarDescending(Point a, Point b)
      {
         return ComparePolar(a, b, isDescending: true);
      }

      int ComparePolar(Point a, Point b, bool isDescending = false)
      {
         var polarA = RectToPolar(a);
         var polarB = RectToPolar(b);
         var center = RectToPolar(location);

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

      double Determinant(Point a, Point b)
      {
         var det = (a.X * b.Y - a.Y * b.X);
         return det;
      }

      Point RectToPolar(Point pt)
      {
         var r = Math.Sqrt(pt.X * pt.X + pt.Y * pt.Y);
         var theta = Math.Atan2(pt.Y, pt.X);

         var polar = new Point(r, theta);
         return polar;
      }

      Point PolarToRect(Point pt)
      {
         var r = pt.X;
         var theta = pt.Y;

         var x = r * Math.Cos(theta);
         var y = r * Math.Sin(theta);

         var rect = new Point(x, y);
         return rect;
      }

      IEnumerable<Point> GetRadiusPoints(Point center, int innerRadius, int outerRadius)
      {
         for (int i = 0; i <= outerRadius; i++)
         {
            if (i < innerRadius) continue;

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
            if (y < innerRadius) continue;

            for (int x = 1; x <= diagonal; x++)
            {
               if (x < innerRadius) continue;

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

      string ToRelativeString(Point pt)
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
         else if(pt.Y < 0)
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
               return string.Format("{0}, {1}", location.X.ToString(), location.Y.ToString());

            case TargetCoordinateUnits.RelativeTile:
               return string.Format("{0}", ToRelativeString(location));

            case TargetCoordinateUnits.RelativeXY:
               return string.Format("Relative {0}, {1}", location.X.ToString(), location.Y.ToString());

            case TargetCoordinateUnits.Self:
               return string.Format("Self");

            case TargetCoordinateUnits.RelativeRadius:
               return string.Format("{0} Tile Radius from {1}",
                  (this.OuterRadius - this.InnerRadius + 1).ToString(),
                  ToRelativeString(this.Location));

            case  TargetCoordinateUnits.AbsoluteRadius:
               return string.Format("{0} Tile Radius from {1}, {2}",
                  (this.OuterRadius - this.InnerRadius + 1).ToString(),
                  this.Location.X.ToString(), this.Location.Y.ToString());
         }

         return string.Empty;
      }
   }
}
