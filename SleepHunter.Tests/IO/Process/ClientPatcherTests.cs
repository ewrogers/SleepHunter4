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

        private static long GetRelativeTarget(byte[] code, nint codeAddress, int instructionOffset)
        {
            var relativeOffset = BitConverter.ToInt32(code, instructionOffset + 1);
            return codeAddress + instructionOffset + 5 + relativeOffset;
        }

        private static int GetShortTarget(byte[] code, int instructionOffset) =>
            instructionOffset + 2 + unchecked((sbyte)code[instructionOffset + 1]);
    }
}
