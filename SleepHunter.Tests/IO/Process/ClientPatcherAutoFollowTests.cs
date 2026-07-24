using SleepHunter.IO.Process;

namespace SleepHunter.Tests.IO.Process
{
    [TestFixture]
    public sealed class ClientPatcherAutoFollowTests
    {
        [TestCase(1)]
        [TestCase(3)]
        [TestCase(10)]
        public void ShouldBuildImprovedAutoFollowStateWithConfiguredDistance(int minimumDistance)
        {
            var state = ClientPatcher.BuildImprovedAutoFollowState(minimumDistance);

            Assert.Multiple(() =>
            {
                Assert.That(state, Has.Length.EqualTo(0x18));
                Assert.That(BitConverter.ToInt32(state, 0x0C), Is.EqualTo(minimumDistance));
                Assert.That(BitConverter.ToInt32(state, 0x10), Is.EqualTo(minimumDistance));
                Assert.That(state[0x14], Is.Zero);
                Assert.That(state[0x15], Is.Zero);
                Assert.That(state[0x16], Is.Zero);
            });
        }

        [TestCase(0)]
        [TestCase(11)]
        public void ShouldRejectOutOfRangeImprovedAutoFollowDistance(int minimumDistance)
        {
            Assert.That(() => ClientPatcher.BuildImprovedAutoFollowState(minimumDistance),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void ShouldBuildImprovedAutoFollowStubWithResolvedStateAddresses()
        {
            var moduleBaseAddress = (nint)0x00400000;
            var stubAddress = (nint)0x10000000;
            var stateAddress = (nint)0x00B70000;

            var stub = ClientPatcher.BuildImprovedAutoFollowStub(
                moduleBaseAddress, stubAddress, stateAddress);

            Assert.Multiple(() =>
            {
                Assert.That(stub, Has.Length.EqualTo(0x1D5));
                Assert.That(stub[0x000..0x009],
                    Is.EqualTo(new byte[] { 0x8B, 0x45, 0x0C, 0xF6, 0x40, 0x2C, 0x04, 0x74, 0x2F }));

                AssertAbsoluteAddresses(stub, 0x00B70000, 0x00B, 0x04F, 0x080, 0x0B6, 0x126);
                AssertAbsoluteAddresses(stub, 0x00B70004, 0x014, 0x08B, 0x0D7, 0x12E);
                AssertAbsoluteAddresses(stub, 0x00B70008, 0x05D, 0x09B, 0x0C4, 0x0CC);
                AssertAbsoluteAddresses(stub, 0x00B7000C, 0x01E, 0x0EE, 0x14E);
                AssertAbsoluteAddresses(stub, 0x00B70010, 0x019);
                AssertAbsoluteAddresses(stub, 0x00B70014, 0x02E, 0x03A, 0x046, 0x074, 0x0AA, 0x161, 0x179);
                AssertAbsoluteAddresses(stub, 0x00B70015, 0x028, 0x065, 0x15B);
                AssertAbsoluteAddresses(stub, 0x00B70016, 0x023);
            });
        }

        [Test]
        public void ShouldBuildImprovedAutoFollowStubWithResolvedClientTargets()
        {
            var moduleBaseAddress = (nint)0x00400000;
            var stubAddress = (nint)0x10000000;
            var stateAddress = (nint)0x00B70000;

            var stub = ClientPatcher.BuildImprovedAutoFollowStub(
                moduleBaseAddress, stubAddress, stateAddress);

            Assert.Multiple(() =>
            {
                AssertRelativeTargets(stub, stubAddress, 0x005F4A70, 0x034, 0x040, 0x16D);
                AssertRelativeTargets(stub, stubAddress, 0x005F44B0, 0x06E);
                AssertRelativeTargets(stub, stubAddress, stubAddress.ToInt64() + 0x190, 0x0A4);
                AssertRelativeTargets(stub, stubAddress, 0x005F4AAD, 0x192);
                AssertRelativeTargets(stub, stubAddress, 0x005F4AA8, 0x197);
                AssertRelativeTargets(stub, stubAddress, 0x005F4900, 0x106, 0x185);
                AssertRelativeTargets(stub, stubAddress, 0x005F4A5B, 0x10B);
                AssertRelativeTargets(stub, stubAddress, 0x005F49EF, 0x11A);
                AssertRelativeTargets(stub, stubAddress, 0x005D48F1, 0x1A1, 0x1C2, 0x1CC);
                AssertRelativeTargets(stub, stubAddress, 0x005D48E5, 0x1AB, 0x1B8, 0x1D1);
            });
        }

        [Test]
        public void ShouldReplayGenerationBranchOutsideOverwrittenHook()
        {
            var moduleBaseAddress = (nint)0x00400000;
            var stubAddress = (nint)0x10000000;
            var stateAddress = (nint)0x00B70000;

            var stub = ClientPatcher.BuildImprovedAutoFollowStub(
                moduleBaseAddress, stubAddress, stateAddress);

            Assert.Multiple(() =>
            {
                Assert.That(stub[0x09F..0x0A4],
                    Is.EqualTo(new byte[] { 0x83, 0x7D, 0x08, 0x00, 0xE9 }));
                Assert.That(GetRelativeOperandTarget(stub, stubAddress, 0x0A4),
                    Is.EqualTo(stubAddress.ToInt64() + 0x190));

                Assert.That(stub[0x190..0x192], Is.EqualTo(new byte[] { 0x0F, 0x85 }));
                Assert.That(GetRelativeOperandTarget(stub, stubAddress, 0x192), Is.EqualTo(0x005F4AAD));
                Assert.That(stub[0x196], Is.EqualTo(0xE9));
                Assert.That(GetRelativeOperandTarget(stub, stubAddress, 0x197), Is.EqualTo(0x005F4AA8));
            });
        }

        [TestCase(0x005EF33A, 0x000, 0xE8, 5)]
        [TestCase(0x005D48DF, 0x19B, 0xE9, 6)]
        [TestCase(0x005F49E5, 0x0A8, 0xE9, 10)]
        [TestCase(0x005F4AA2, 0x072, 0xE9, 6)]
        [TestCase(0x005F4D0E, 0x044, 0xE8, 5)]
        public void ShouldBuildImprovedAutoFollowHooks(int hookAddressValue, int stubOffset,
            byte opcode, int hookLength)
        {
            var hookAddress = (nint)hookAddressValue;
            var stubAddress = (nint)0x10000000;

            var hook = ClientPatcher.BuildImprovedAutoFollowHook(
                hookAddress, stubAddress, stubOffset, opcode, hookLength);

            Assert.Multiple(() =>
            {
                Assert.That(hook, Has.Length.EqualTo(hookLength));
                Assert.That(hook[0], Is.EqualTo(opcode));
                Assert.That(GetRelativeOperandTarget(hook, hookAddress, 1),
                    Is.EqualTo(stubAddress.ToInt64() + stubOffset));
                Assert.That(hook[5..], Is.All.EqualTo(0x90));
            });
        }

        [Test]
        public void ShouldAllowOnlyShiftPursuitDispatchForLivingObjects()
        {
            var moduleBaseAddress = (nint)0x00400000;
            var stubAddress = (nint)0x10000000;
            var stateAddress = (nint)0x00B70000;

            var stub = ClientPatcher.BuildImprovedAutoFollowStub(
                moduleBaseAddress, stubAddress, stateAddress);

            Assert.Multiple(() =>
            {
                Assert.That(stub[0x19B..0x19F],
                    Is.EqualTo(new byte[] { 0x83, 0x7D, 0xA0, 0x08 }));
                Assert.That(GetRelativeOperandTarget(stub, stubAddress, 0x1A1), Is.EqualTo(0x005D48F1));

                Assert.That(stub[0x1A5..0x1A9],
                    Is.EqualTo(new byte[] { 0x80, 0x7D, 0xC3, 0x00 }));
                Assert.That(GetRelativeOperandTarget(stub, stubAddress, 0x1AB), Is.EqualTo(0x005D48E5));

                Assert.That(stub[0x1AF..0x1B6],
                    Is.EqualTo(new byte[] { 0x8B, 0x45, 0x0C, 0x80, 0x78, 0x0C, 0x05 }));
                Assert.That(GetRelativeOperandTarget(stub, stubAddress, 0x1B8), Is.EqualTo(0x005D48E5));

                Assert.That(stub[0x1BC..0x1C0],
                    Is.EqualTo(new byte[] { 0x83, 0x7D, 0xA0, 0x01 }));
                Assert.That(GetRelativeOperandTarget(stub, stubAddress, 0x1C2), Is.EqualTo(0x005D48F1));
                Assert.That(stub[0x1C6..0x1CA],
                    Is.EqualTo(new byte[] { 0x83, 0x7D, 0xA0, 0x02 }));
                Assert.That(GetRelativeOperandTarget(stub, stubAddress, 0x1CC), Is.EqualTo(0x005D48F1));
                Assert.That(stub[0x1D0], Is.EqualTo(0xE9));
                Assert.That(GetRelativeOperandTarget(stub, stubAddress, 0x1D1), Is.EqualTo(0x005D48E5));
            });
        }

        private static void AssertAbsoluteAddresses(byte[] stub, uint expected, params int[] offsets)
        {
            foreach (var offset in offsets)
            {
                Assert.That(BitConverter.ToUInt32(stub, offset), Is.EqualTo(expected),
                    $"Absolute address at stub + 0x{offset:X3}");
            }
        }

        private static void AssertRelativeTargets(byte[] stub, nint stubAddress, long expected,
            params int[] offsets)
        {
            foreach (var offset in offsets)
            {
                Assert.That(GetRelativeOperandTarget(stub, stubAddress, offset), Is.EqualTo(expected),
                    $"Relative target at stub + 0x{offset:X3}");
            }
        }

        private static long GetRelativeOperandTarget(byte[] code, nint codeAddress, int operandOffset)
        {
            var relativeOffset = BitConverter.ToInt32(code, operandOffset);
            return codeAddress.ToInt64() + operandOffset + sizeof(int) + relativeOffset;
        }
    }
}
