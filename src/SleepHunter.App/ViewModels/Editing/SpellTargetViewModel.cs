using System;
using System.Text;
using System.Windows;

using CommunityToolkit.Mvvm.ComponentModel;

namespace SleepHunter.ViewModels.Editing
{
    public sealed class SpellTargetViewModel :
        ObservableObject
    {
        private SpellTargetMode unitType;
        private string characterName;
        private Point location = new();
        private Point offset = new();
        private int innerRadius;
        private int outerRadius;
        public SpellTargetMode Mode
        {
            get => unitType;
            set => SetProperty(ref unitType, value);
        }

        public string CharacterName
        {
            get => characterName;
            set => SetProperty(ref characterName, value);
        }

        public Point Location
        {
            get => location;
            set => SetProperty(ref location, value);
        }

        public Point Offset
        {
            get => offset;
            set => SetProperty(ref offset, value);
        }

        public int InnerRadius
        {
            get => innerRadius;
            set => SetProperty(ref innerRadius, value);
        }

        public int OuterRadius
        {
            get => outerRadius;
            set => SetProperty(ref outerRadius, value);
        }

        public SpellTargetViewModel()
           : this(SpellTargetMode.None, new Point(), new Point()) { }

        public SpellTargetViewModel(
            SpellTargetMode units,
            Point location)
           : this(units, location, new Point()) { }

        public SpellTargetViewModel(
            SpellTargetMode units,
            Point location,
            Point offset)
        {
            unitType = units;
            this.location = location;
            this.offset = offset;
        }

        private static string ToRelativeString(Point pt)
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
            return unitType switch
            {
                SpellTargetMode.None => null,
                SpellTargetMode.Character => string.Format("{0}", characterName),
                SpellTargetMode.AbsoluteTile => string.Format("Tile {0}, {1}", location.X.ToString(), location.Y.ToString()),
                SpellTargetMode.AbsoluteXY => string.Format("Screen {0}, {1}", location.X.ToString(), location.Y.ToString()),
                SpellTargetMode.RelativeTile => string.Format("{0}", ToRelativeString(location)),
                SpellTargetMode.Self => string.Format("Self"),
                SpellTargetMode.RelativeRadius => string.Format("{0} Tile Radius from {1}",
                                       (OuterRadius - InnerRadius + 1).ToString(),
                                       ToRelativeString(Location)),
                SpellTargetMode.AbsoluteRadius => string.Format("{0} Tile Radius from {1}, {2}",
                                       (OuterRadius - InnerRadius + 1).ToString(),
                                       Location.X.ToString(), Location.Y.ToString()),
                _ => string.Empty,
            };
        }
    }
}
