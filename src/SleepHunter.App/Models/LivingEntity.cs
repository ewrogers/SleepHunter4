using System;

namespace SleepHunter.Models
{
    public sealed record LivingEntity
    {
        public uint Id { get; init; }

        public string Name { get; init; }

        public int X { get; init; }

        public int Y { get; init; }

        public WorldEntityKind Kind { get; init; }

        public byte? CreatureType { get; init; }

        public byte Direction { get; init; }

        public bool IsLocalPlayer { get; init; }

        public bool IsGroupMember { get; init; }

        public string RuntimeClass { get; init; }

        public double DistanceFrom(int x, int y)
        {
            var deltaX = (long)X - x;
            var deltaY = (long)Y - y;
            return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        }
    }
}
