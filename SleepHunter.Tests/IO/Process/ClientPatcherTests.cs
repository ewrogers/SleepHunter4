using SleepHunter.IO.Process;

namespace SleepHunter.Tests.IO.Process
{
    [TestFixture]
    public sealed class ClientPatcherTests
    {
        [Test]
        public void ShouldBuildModifiersKeyFixStubWithResolvedAddresses()
        {
            var moduleBaseAddress = (nint)0x00400000;
            var stubAddress = (nint)0x01000000;

            var stub = ClientPatcher.BuildModifiersKeyFixStub(moduleBaseAddress, stubAddress);

            Assert.Multiple(() =>
            {
                Assert.That(stub, Has.Length.EqualTo(68));
                Assert.That(stub[0..2], Is.EqualTo(new byte[] { 0x9C, 0x60 }));
                Assert.That(GetRelativeTarget(stub, stubAddress, 2), Is.EqualTo(0x00427380));
                Assert.That(stub[7..13], Is.EqualTo(new byte[] { 0x85, 0xC0, 0x74, 0x32, 0x89, 0xC3 }));
                Assert.That(GetShortTarget(stub, 9), Is.EqualTo(61));
                Assert.That(stub[13], Is.EqualTo(0xE8));
                Assert.That(GetRelativeTarget(stub, stubAddress, 13), Is.EqualTo(0x0062006E));
                Assert.That(stub[22..32],
                    Is.EqualTo(new byte[] { 0xF6, 0x84, 0x33, 0x34, 0x03, 0x00, 0x00, 0x80, 0x74, 0x0D }));
                Assert.That(GetShortTarget(stub, 30), Is.EqualTo(45));
                Assert.That(stub[32..40],
                    Is.EqualTo(new byte[] { 0x6A, 0x00, 0x57, 0x6A, 0x00, 0x56, 0x89, 0xD9 }));
                Assert.That(stub[40], Is.EqualTo(0xE8));
                Assert.That(GetRelativeTarget(stub, stubAddress, 40), Is.EqualTo(0x00466E60));
                Assert.That(stub[45..61], Is.EqualTo(new byte[]
                {
                    0x46, 0x81, 0xFE, 0x00, 0x01, 0x00, 0x00, 0x7C, 0xE0,
                    0xC6, 0x83, 0x34, 0x04, 0x00, 0x00, 0x00
                }));
                Assert.That(GetShortTarget(stub, 52), Is.EqualTo(22));
                Assert.That(stub[61..63], Is.EqualTo(new byte[] { 0x61, 0x9D }));
                Assert.That(stub[63], Is.EqualTo(0xE9));
                Assert.That(GetRelativeTarget(stub, stubAddress, 63), Is.EqualTo(0x004AC950));
            });
        }

        [Test]
        public void ShouldBuildFiveByteCallToModifiersKeyFixStub()
        {
            var callAddress = (nint)0x004A9D81;
            var stubAddress = (nint)0x01000000;

            var call = ClientPatcher.BuildModifiersKeyFixCall(callAddress, stubAddress);

            Assert.Multiple(() =>
            {
                Assert.That(call, Has.Length.EqualTo(5));
                Assert.That(call[0], Is.EqualTo(0xE8));
                Assert.That(GetRelativeTarget(call, callAddress, 0), Is.EqualTo(stubAddress.ToInt64()));
            });
        }

        [Test]
        public void ShouldBuildShowItemQuantitiesInDialogsStubWithResolvedAddresses()
        {
            var moduleBaseAddress = (nint)0x00400000;
            var stubAddress = (nint)0x10000000;

            var stub = ClientPatcher.BuildShowItemQuantitiesInDialogsStub(moduleBaseAddress, stubAddress);

            Assert.Multiple(() =>
            {
                Assert.That(stub, Has.Length.EqualTo(341));
                Assert.That(stub[0x0C..0x23], Is.EqualTo(new byte[]
                {
                    0x8B, 0x75, 0x10, 0x89, 0xB5, 0xD8, 0xFE, 0xFF, 0xFF, 0x8B, 0x7D, 0x00,
                    0x8B, 0x7F, 0xE8, 0x0F, 0xB6, 0x3F, 0x90, 0x90, 0x90, 0x90, 0x90
                }));
                Assert.That(GetNearConditionalTarget(stub, 0x26), Is.EqualTo(0x139));
                Assert.That(GetNearConditionalTarget(stub, 0x2F), Is.EqualTo(0x139));
                Assert.That(GetNearConditionalTarget(stub, 0x3C), Is.EqualTo(0x139));
                Assert.That(stub[0x72..0x7A],
                    Is.EqualTo(new byte[] { 0x49, 0x0F, 0x8E, 0xC0, 0x00, 0x00, 0x00, 0x41 }));
                Assert.That(GetNearConditionalTarget(stub, 0x73), Is.EqualTo(0x139));
                Assert.That(GetRelativeTarget(stub, stubAddress, 0x35), Is.EqualTo(0x005A9C40));
                Assert.That(BitConverter.ToUInt32(stub, 0x99), Is.EqualTo(0x00669380));
                Assert.That(BitConverter.ToInt32(stub, 0xCC), Is.EqualTo(20));
                Assert.That(stub[0xDA..0xDD], Is.EqualTo(new byte[] { 0x83, 0xEA, 0x02 }));
                Assert.That(stub[0x109..0x115], Is.EqualTo(new byte[]
                {
                    0x66, 0xC7, 0x07, 0x2E, 0x2E, 0x90, 0x90, 0x90, 0x90, 0x83, 0xC7, 0x02
                }));
                Assert.That(GetRelativeTarget(stub, stubAddress, 0xF3), Is.EqualTo(0x0047D670));
                Assert.That(stub[0x133..0x139],
                    Is.EqualTo(new byte[] { 0x89, 0x85, 0xD8, 0xFE, 0xFF, 0xFF }));
                Assert.That(stub[0x139..0x145], Is.EqualTo(new byte[]
                {
                    0xFF, 0xB5, 0xD8, 0xFE, 0xFF, 0xFF, 0xFF, 0x75, 0x0C, 0xFF, 0x75, 0x08
                }));
                Assert.That(GetRelativeTarget(stub, stubAddress, 0x145), Is.EqualTo(0x006228C4));
                Assert.That(stub[^8..],
                    Is.EqualTo(new byte[] { 0x8D, 0x65, 0xF4, 0x5F, 0x5E, 0x5B, 0x5D, 0xC3 }));
            });
        }

        [Test]
        public void ShouldBuildFiveByteCallToShowItemQuantitiesInDialogsStub()
        {
            var callAddress = (nint)0x0053609C;
            var stubAddress = (nint)0x10000000;

            var call = ClientPatcher.BuildShowItemQuantitiesInDialogsCall(callAddress, stubAddress);

            Assert.Multiple(() =>
            {
                Assert.That(call, Is.EqualTo(new byte[] { 0xE8, 0x5F, 0x9F, 0xAC, 0x0F }));
                Assert.That(GetRelativeTarget(call, callAddress, 0), Is.EqualTo(stubAddress.ToInt64()));
            });
        }

        [Test]
        public void ShouldBuildGroundItemCollectorStubWithResolvedAddresses()
        {
            var moduleBaseAddress = (nint)0x00400000;
            var stubAddress = (nint)0x10000000;
            var stateAddress = (nint)0x00B70000;

            var stub = ClientPatcher.BuildGroundItemCollectorStub(
                moduleBaseAddress, stubAddress, stateAddress);

            Assert.Multiple(() =>
            {
                Assert.That(stub, Has.Length.EqualTo(157));
                Assert.That(stub[0..11], Is.EqualTo(new byte[]
                {
                    0x55, 0x89, 0xE5, 0x83, 0xEC, 0x08, 0x53, 0x56, 0x57, 0x89, 0xCE
                }));
                Assert.That(BitConverter.ToUInt32(stub, 0x3A), Is.EqualTo(0x0068B1AC));
                Assert.That(BitConverter.ToUInt32(stub, 0x4A), Is.EqualTo(0x00B70000));
                Assert.That(stub[0x4E..0x53], Is.EqualTo(new byte[] { 0x3D, 0xFF, 0x00, 0x00, 0x00 }));
                Assert.That(BitConverter.ToUInt32(stub, 0x5A), Is.EqualTo(0x00B70100));
                Assert.That(BitConverter.ToUInt32(stub, 0x70), Is.EqualTo(0x00B70000));
                Assert.That(BitConverter.ToUInt32(stub, 0x76), Is.EqualTo(0x00B70004));
                Assert.That(BitConverter.ToUInt32(stub, 0x7E), Is.EqualTo(0x00B70008));
                Assert.That(GetRelativeTarget(stub, stubAddress, 0x98), Is.EqualTo(0x005D3745));
            });
        }

        [Test]
        public void ShouldBuildGroundItemFrameStubWithResolvedAddresses()
        {
            var moduleBaseAddress = (nint)0x00400000;
            var stubAddress = (nint)0x20000000;
            var stateAddress = (nint)0x00B70000;

            var stub = ClientPatcher.BuildGroundItemFrameStub(moduleBaseAddress, stubAddress, stateAddress);

            Assert.Multiple(() =>
            {
                Assert.That(stub, Has.Length.EqualTo(186));
                Assert.That(BitConverter.ToUInt32(stub, 0x0D), Is.EqualTo(0x00B70028));
                Assert.That(BitConverter.ToUInt32(stub, 0x13), Is.EqualTo(0x00B70000));
                Assert.That(GetRelativeTarget(stub, stubAddress, 0x25), Is.EqualTo(0x00427380));
                Assert.That(stub[0x2E..0x35],
                    Is.EqualTo(new byte[] { 0xF6, 0x80, 0x34, 0x04, 0x00, 0x00, 0x01 }));
                Assert.That(BitConverter.ToUInt32(stub, 0x3B), Is.EqualTo(0x00B70000));
                Assert.That(BitConverter.ToUInt32(stub, 0x46), Is.EqualTo(0x00B70100));
                Assert.That(BitConverter.ToUInt32(stub, 0x52), Is.EqualTo(0x0068B1AC));
                Assert.That(BitConverter.ToUInt32(stub, 0x79), Is.EqualTo(0x00B70004));
                Assert.That(BitConverter.ToUInt32(stub, 0x8D), Is.EqualTo(0x00B70008));
                Assert.That(GetRelativeTarget(stub, stubAddress, 0x91), Is.EqualTo(0x005D3190));
                Assert.That(GetRelativeTarget(stub, stubAddress, 0xB5), Is.EqualTo(0x005CE286));
            });
        }

        [TestCase(false, 0x00467C15)]
        [TestCase(true, 0x00467E35)]
        public void ShouldBuildRawAltKeyInvalidationStubs(bool keyUp, int expectedContinuation)
        {
            var moduleBaseAddress = (nint)0x00400000;
            var stubAddress = (nint)0x20000000;
            var stateAddress = (nint)0x00B70000;

            var stub = ClientPatcher.BuildGroundItemKeyTransitionStub(
                moduleBaseAddress, stubAddress, stateAddress, keyUp);

            Assert.Multiple(() =>
            {
                Assert.That(stub, Has.Length.EqualTo(84));
                Assert.That(stub[0x1F..0x2C], Is.EqualTo(new byte[]
                {
                    0x0F, 0xB6, 0x45, 0x08, 0x83, 0xF8, 0x38, 0x74, 0x07, 0x3D, 0xB8, 0x00, 0x00
                }));
                Assert.That(BitConverter.ToUInt32(stub, 0x31), Is.EqualTo(0x00B70028));
                Assert.That(GetRelativeTarget(stub, stubAddress, 0x3B), Is.EqualTo(0x00549F60));
                Assert.That(GetRelativeTarget(stub, stubAddress, 0x4F), Is.EqualTo(expectedContinuation));
            });
        }

        [Test]
        public void ShouldBuildPaddedJumpsToGroundItemStubs()
        {
            var hookAddress = (nint)0x005D3740;
            var stubAddress = (nint)0x10000000;

            var fiveByteHook = ClientPatcher.BuildGroundItemHook(hookAddress, stubAddress, 5);
            var sixByteHook = ClientPatcher.BuildGroundItemHook(hookAddress, stubAddress, 6);

            Assert.Multiple(() =>
            {
                Assert.That(fiveByteHook, Has.Length.EqualTo(5));
                Assert.That(fiveByteHook[0], Is.EqualTo(0xE9));
                Assert.That(GetRelativeTarget(fiveByteHook, hookAddress, 0), Is.EqualTo(stubAddress.ToInt64()));
                Assert.That(sixByteHook, Has.Length.EqualTo(6));
                Assert.That(sixByteHook[5], Is.EqualTo(0x90));
                Assert.That(GetRelativeTarget(sixByteHook, hookAddress, 0), Is.EqualTo(stubAddress.ToInt64()));
            });
        }

        [Test]
        public void ShouldLeaveStaticRenderModeSelectorUnchanged()
        {
            var expected = ClientPatcher.GetExpectedStaticRenderModeSelector();

            Assert.That(expected, Is.EqualTo(new byte[]
            {
                0x8B, 0x55, 0xD0, 0x0F, 0xB6, 0x82, 0xB9, 0x00, 0x00, 0x00, 0x25, 0x80, 0x00, 0x00, 0x00, 0x74,
                0x09, 0xC7, 0x45, 0xE8, 0x6D, 0x00, 0x00, 0x00, 0xEB, 0x16, 0x8B, 0x4D, 0xD0, 0x0F, 0xB6, 0x91,
                0xB9, 0x00, 0x00, 0x00, 0x83, 0xE2, 0x40, 0x74, 0x07, 0xC7, 0x45, 0xE8, 0x03, 0x00, 0x00, 0x00
            }));
        }

        private static long GetRelativeTarget(byte[] code, nint codeAddress, int instructionOffset)
        {
            var relativeOffset = BitConverter.ToInt32(code, instructionOffset + 1);
            return codeAddress + instructionOffset + 5 + relativeOffset;
        }

        private static int GetShortTarget(byte[] code, int instructionOffset) =>
            instructionOffset + 2 + unchecked((sbyte)code[instructionOffset + 1]);

        private static int GetNearConditionalTarget(byte[] code, int instructionOffset) =>
            instructionOffset + 6 + BitConverter.ToInt32(code, instructionOffset + 2);
    }
}
