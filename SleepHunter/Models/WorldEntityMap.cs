using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

using SleepHunter.Common;
using SleepHunter.IO.Process;

namespace SleepHunter.Models
{
    public sealed class WorldEntityMap : UpdatableObject
    {
        private const string WorldObjectListKey = @"WorldObjectList";
        private const int MaximumNodeCount = 4096;
        private const int NodeSize = 0x18;
        private const int LivingObjectSnapshotSize = 0x1ED;

        private readonly Stream stream;
        private readonly BinaryReader reader;

        private IReadOnlyDictionary<uint, LivingEntity> knownEntities =
            new ReadOnlyDictionary<uint, LivingEntity>(new Dictionary<uint, LivingEntity>());

        public Player Owner { get; init; }

        public IReadOnlyDictionary<uint, LivingEntity> KnownEntities
        {
            get => knownEntities;
            private set
            {
                if (SetProperty(ref knownEntities, value))
                {
                    RaisePropertyChanged(nameof(Players));
                    RaisePropertyChanged(nameof(Monsters));
                    RaisePropertyChanged(nameof(GroupMembers));
                }
            }
        }

        public IEnumerable<LivingEntity> Players =>
            KnownEntities.Values.Where(entity => entity.Kind == WorldEntityKind.Player);

        public IEnumerable<LivingEntity> Monsters =>
            KnownEntities.Values.Where(entity => entity.Kind == WorldEntityKind.Monster);

        public IEnumerable<LivingEntity> GroupMembers =>
            KnownEntities.Values.Where(entity => entity.IsGroupMember);

        public WorldEntityMap(Player owner)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));

            stream = owner.Accessor.GetStream();
            reader = new BinaryReader(stream, Encoding.ASCII);
        }

        public LivingEntity FindNearest(
            Func<LivingEntity, bool> predicate,
            bool includeLocalPlayer = false)
        {
            CheckIfDisposed();

            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            return KnownEntities.Values
                .Where(entity => (includeLocalPlayer || !entity.IsLocalPlayer) && predicate(entity))
                .OrderBy(entity => DistanceSquared(entity, Owner.Location.X, Owner.Location.Y))
                .FirstOrDefault();
        }

        public LivingEntity FindNearestPlayer() =>
            FindNearest(entity => entity.Kind == WorldEntityKind.Player);

        public LivingEntity FindNearestMonster() =>
            FindNearest(entity => entity.Kind == WorldEntityKind.Monster);

        public LivingEntity FindNearestGroupMember() =>
            FindNearest(entity => entity.IsGroupMember);

        protected override void OnUpdate()
        {
            var layout = Owner.Layout;
            if (layout == null ||
                !layout.TryGetVariable(WorldObjectListKey, out var listVariable) ||
                !listVariable.TryDereferenceValue(reader, out var listAddress))
            {
                ResetDefaults();
                return;
            }

            var groupNames = Owner.Profile.GroupMemberNames;
            if (!TryReadKnownEntities(reader, listAddress, groupNames, out var entities))
                return;

            // The client can rebuild the world while it is being polled. Only publish a
            // snapshot if the list generation is still the one that was traversed.
            if (!listVariable.TryDereferenceValue(reader, out var currentListAddress) ||
                currentListAddress != listAddress)
            {
                return;
            }

            KnownEntities = new ReadOnlyDictionary<uint, LivingEntity>(entities);
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposed)
                return;

            if (isDisposing)
            {
                reader?.Dispose();
                stream?.Dispose();
            }

            base.Dispose(isDisposing);
        }

        internal static bool TryReadKnownEntities(
            BinaryReader reader,
            long listAddress,
            IEnumerable<string> groupMemberNames,
            out Dictionary<uint, LivingEntity> entities)
        {
            entities = new Dictionary<uint, LivingEntity>();

            if (!RuntimeMemoryReader.TryReadUInt32(reader, listAddress + 0x20, out var headAddress) ||
                !RuntimeMemoryReader.TryReadBytes(reader, headAddress, NodeSize, out var headSnapshot))
            {
                return false;
            }

            var rootAddress = BinaryPrimitives.ReadUInt32LittleEndian(headSnapshot.AsSpan(0x04, 4));
            var groupNames = new HashSet<string>(
                groupMemberNames ?? Array.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);
            var pending = new Stack<uint>();
            var visited = new HashSet<uint>();
            pending.Push(rootAddress);

            while (pending.Count > 0)
            {
                var nodeAddress = pending.Pop();
                if (nodeAddress == 0 || nodeAddress == headAddress)
                    continue;

                if (visited.Count >= MaximumNodeCount ||
                    !visited.Add(nodeAddress) ||
                    !RuntimeMemoryReader.TryReadBytes(reader, nodeAddress, NodeSize, out var node))
                {
                    entities.Clear();
                    return false;
                }

                var left = BinaryPrimitives.ReadUInt32LittleEndian(node.AsSpan(0x00, 4));
                var right = BinaryPrimitives.ReadUInt32LittleEndian(node.AsSpan(0x08, 4));
                var nodeId = BinaryPrimitives.ReadUInt32LittleEndian(node.AsSpan(0x0C, 4));
                var objectAddress = BinaryPrimitives.ReadUInt32LittleEndian(node.AsSpan(0x10, 4));

                pending.Push(right);
                pending.Push(left);

                if (TryReadLivingEntity(reader, objectAddress, nodeId, groupNames, out var entity))
                    entities[entity.Id] = entity;
            }

            return true;
        }

        private static bool TryReadLivingEntity(
            BinaryReader reader,
            uint objectAddress,
            uint nodeId,
            ISet<string> groupNames,
            out LivingEntity entity)
        {
            entity = null;

            if (!RuntimeMemoryReader.TryReadRttiClassName(reader, objectAddress, out var runtimeClass) ||
                !IsLivingRuntimeClass(runtimeClass) ||
                !RuntimeMemoryReader.TryReadBytes(
                    reader,
                    objectAddress,
                    LivingObjectSnapshotSize,
                    out var snapshot))
            {
                return false;
            }

            var id = BinaryPrimitives.ReadUInt32LittleEndian(snapshot.AsSpan(0x24, 4));
            if (id == 0 || (nodeId != 0 && id != nodeId) || snapshot[0x48] == 0)
                return false;

            var isHuman = IsHumanRuntimeClass(runtimeClass);
            var creatureType = isHuman ? (byte?)null : snapshot[0x1EC];
            var kind = isHuman
                ? WorldEntityKind.Player
                : GetMonsterKind(creatureType.Value);
            var name = ReadAscii(snapshot.AsSpan(0x112, 0x80));

            if (string.IsNullOrWhiteSpace(name))
            {
                var namePaneAddress = BinaryPrimitives.ReadUInt32LittleEndian(snapshot.AsSpan(0x58, 4));
                RuntimeMemoryReader.TryReadAsciiString(
                    reader,
                    namePaneAddress + 0x198,
                    0x40,
                    out name);
            }

            name = SanitizeDisplayName(name);
            entity = new LivingEntity
            {
                Id = id,
                Name = name,
                X = BinaryPrimitives.ReadInt32LittleEndian(snapshot.AsSpan(0x44, 4)),
                Y = BinaryPrimitives.ReadInt32LittleEndian(snapshot.AsSpan(0x40, 4)),
                Kind = kind,
                CreatureType = creatureType,
                Direction = snapshot[0x192],
                IsLocalPlayer = isHuman && snapshot[0x98] != 0,
                IsGroupMember = !string.IsNullOrWhiteSpace(name) && groupNames.Contains(name),
                RuntimeClass = runtimeClass
            };
            return true;
        }

        internal static WorldEntityKind GetMonsterKind(byte creatureType) =>
            creatureType switch
            {
                1 => WorldEntityKind.Passable,
                2 => WorldEntityKind.Mundane,
                3 => WorldEntityKind.Solid,
                4 => WorldEntityKind.Player,
                _ => WorldEntityKind.Monster
            };

        private static bool IsLivingRuntimeClass(string runtimeClass) =>
            IsHumanRuntimeClass(runtimeClass) ||
            runtimeClass.EndsWith("WorldObject_Monster", StringComparison.Ordinal);

        private static bool IsHumanRuntimeClass(string runtimeClass) =>
            runtimeClass.EndsWith("WorldObject_Human", StringComparison.Ordinal) ||
            runtimeClass.EndsWith("WorldObject_User", StringComparison.Ordinal);

        private static string ReadAscii(ReadOnlySpan<byte> bytes)
        {
            var terminator = bytes.IndexOf((byte)0);
            if (terminator >= 0)
                bytes = bytes[..terminator];

            return Encoding.ASCII.GetString(bytes);
        }

        internal static string SanitizeDisplayName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            var trimmed = name.Trim();
            return trimmed.All(character => character is >= ' ' and <= '~')
                ? trimmed
                : null;
        }

        private static long DistanceSquared(LivingEntity entity, int x, int y)
        {
            var deltaX = (long)entity.X - x;
            var deltaY = (long)entity.Y - y;
            return deltaX * deltaX + deltaY * deltaY;
        }

        private void ResetDefaults()
        {
            if (KnownEntities.Count == 0)
                return;

            KnownEntities =
                new ReadOnlyDictionary<uint, LivingEntity>(new Dictionary<uint, LivingEntity>());
        }
    }
}
