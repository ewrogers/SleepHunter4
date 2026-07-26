using System.Buffers.Binary;
using System.Text;

using SleepHunter.IO.Process;

namespace SleepHunter.Tests.IO.Process
{
    [TestFixture]
    public sealed class MemoryVariableTests
    {
        [Test]
        public void ShouldReadTypedUInt32Values()
        {
            var memory = new byte[16];
            BinaryPrimitives.WriteUInt32LittleEndian(memory.AsSpan(4), 0xFEDCBA98);
            var variable = new MemoryVariable("Value", 4)
            {
                ValueType = MemoryValueType.UInt32
            };

            using var reader = new BinaryReader(new MemoryStream(memory));
            var wasRead = variable.TryReadInteger(reader, out var value);

            Assert.Multiple(() =>
            {
                Assert.That(wasRead, Is.True);
                Assert.That(value, Is.EqualTo(0xFEDCBA98L));
            });
        }

        [Test]
        public void ShouldKeepStringIntegersAsTheBackwardCompatibleDefault()
        {
            var memory = new byte[16];
            Encoding.ASCII.GetBytes("12345\0").CopyTo(memory.AsSpan(4));
            var variable = new MemoryVariable("Value", 4, maxLength: 8);

            using var reader = new BinaryReader(new MemoryStream(memory), Encoding.ASCII);
            var wasRead = variable.TryReadInteger(reader, out var value);

            Assert.Multiple(() =>
            {
                Assert.That(variable.ValueType, Is.EqualTo(MemoryValueType.String));
                Assert.That(wasRead, Is.True);
                Assert.That(value, Is.EqualTo(12345));
            });
        }

        [Test]
        public void ShouldPreserveSignedByteValues()
        {
            var memory = new byte[8];
            memory[4] = unchecked((byte)-12);
            var variable = new MemoryVariable("Value", 4)
            {
                ValueType = MemoryValueType.SByte
            };

            using var reader = new BinaryReader(new MemoryStream(memory));
            var wasRead = variable.TryReadInteger(reader, out var value);

            Assert.Multiple(() =>
            {
                Assert.That(wasRead, Is.True);
                Assert.That(value, Is.EqualTo(-12));
            });
        }

        [Test]
        public void ShouldFollowTheAdjustedWorldUserPointerChain()
        {
            const int rootAddress = 0x400000;
            const int adjustedWorldInterface = 0x400300;
            const int worldUserPointerAddress = adjustedWorldInterface - 0x20;
            const int worldUserAddress = 0x401000;
            const int healthAddress = worldUserAddress + 0x1078;

            var memory = new byte[healthAddress + sizeof(uint)];
            BinaryPrimitives.WriteUInt32LittleEndian(memory.AsSpan(rootAddress), adjustedWorldInterface);
            BinaryPrimitives.WriteUInt32LittleEndian(memory.AsSpan(worldUserPointerAddress), worldUserAddress);
            BinaryPrimitives.WriteUInt32LittleEndian(memory.AsSpan(healthAddress), 123456);

            var variable = new DynamicMemoryVariable("CurrentHealth", rootAddress)
            {
                ValueType = MemoryValueType.UInt32,
                Offsets =
                {
                    new MemoryOffset(-0x20),
                    new MemoryOffset(0x1078)
                }
            };

            using var reader = new BinaryReader(new MemoryStream(memory));
            var wasRead = variable.TryReadInteger(reader, out var value);

            Assert.Multiple(() =>
            {
                Assert.That(wasRead, Is.True);
                Assert.That(value, Is.EqualTo(123456));
            });
        }
    }
}
