using System.Buffers.Binary;
using System.Text;

using SleepHunter.Models;

namespace SleepHunter.Tests.Models
{
    [TestFixture]
    public sealed class WorldEntityMapTests
    {
        [Test]
        public void ShouldWalkAndClassifyAKnownHumanEntity()
        {
            const uint listAddress = 0x00400000;
            const uint headAddress = 0x00401000;
            const uint nodeAddress = 0x00402000;
            const uint objectAddress = 0x00403000;
            const uint virtualTableAddress = 0x00404000;
            const uint locatorAddress = 0x00405000;
            const uint typeDescriptorAddress = 0x00406000;

            var memory = new byte[0x00407000];
            WriteUInt32(memory, listAddress + 0x20, headAddress);
            WriteUInt32(memory, headAddress + 0x04, nodeAddress);
            WriteUInt32(memory, nodeAddress + 0x00, headAddress);
            WriteUInt32(memory, nodeAddress + 0x08, headAddress);
            WriteUInt32(memory, nodeAddress + 0x0C, 42);
            WriteUInt32(memory, nodeAddress + 0x10, objectAddress);

            WriteUInt32(memory, objectAddress, virtualTableAddress);
            WriteUInt32(memory, virtualTableAddress - 4, locatorAddress);
            WriteUInt32(memory, locatorAddress + 0x0C, typeDescriptorAddress);
            WriteAscii(memory, typeDescriptorAddress + 0x08, ".?AVWorldObject_Human@@");

            WriteUInt32(memory, objectAddress + 0x24, 42);
            WriteInt32(memory, objectAddress + 0x40, 20);
            WriteInt32(memory, objectAddress + 0x44, 10);
            memory[objectAddress + 0x48] = 1;
            WriteAscii(memory, objectAddress + 0x112, "Alice");
            memory[objectAddress + 0x192] = 3;

            using var stream = new MemoryStream(memory, writable: false);
            using var reader = new BinaryReader(stream, Encoding.ASCII);

            var success = WorldEntityMap.TryReadKnownEntities(
                reader,
                listAddress,
                new[] { "Alice" },
                out var entities);

            Assert.That(success, Is.True);
            Assert.That(entities, Has.Count.EqualTo(1));
            var entity = entities[42];
            Assert.Multiple(() =>
            {
                Assert.That(entity.Name, Is.EqualTo("Alice"));
                Assert.That(entity.X, Is.EqualTo(10));
                Assert.That(entity.Y, Is.EqualTo(20));
                Assert.That(entity.Direction, Is.EqualTo(3));
                Assert.That(entity.Kind, Is.EqualTo(WorldEntityKind.Player));
                Assert.That(entity.IsGroupMember, Is.True);
                Assert.That(entity.RuntimeClass, Is.EqualTo("WorldObject_Human"));
            });
        }

        [TestCase(0, WorldEntityKind.Monster)]
        [TestCase(1, WorldEntityKind.Passable)]
        [TestCase(2, WorldEntityKind.Mundane)]
        [TestCase(3, WorldEntityKind.Solid)]
        [TestCase(4, WorldEntityKind.Player)]
        public void ShouldMapDocumentedCreatureTypes(byte creatureType, WorldEntityKind expected)
        {
            Assert.That(WorldEntityMap.GetMonsterKind(creatureType), Is.EqualTo(expected));
        }

        private static void WriteUInt32(byte[] memory, uint address, uint value) =>
            BinaryPrimitives.WriteUInt32LittleEndian(memory.AsSpan((int)address, 4), value);

        private static void WriteInt32(byte[] memory, uint address, int value) =>
            BinaryPrimitives.WriteInt32LittleEndian(memory.AsSpan((int)address, 4), value);

        private static void WriteAscii(byte[] memory, uint address, string value)
        {
            Encoding.ASCII.GetBytes(value).CopyTo(memory.AsSpan((int)address));
            memory[address + value.Length] = 0;
        }
    }
}
