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
