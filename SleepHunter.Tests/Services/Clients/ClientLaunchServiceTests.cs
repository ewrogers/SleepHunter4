using System.Text;
using SleepHunter.Services.Clients;
using SleepHunter.Settings;
using SleepHunter.Tests.Support;

namespace SleepHunter.Tests.Services.Clients
{
    public sealed class ClientLaunchServiceTests
    {
        private const long FirstAddress = 0x4B897C;
        private const long SecondAddress = 0x4B8ACF;
        private const long ThirdAddress = 0x564855;

        [Test]
        public void ShouldRejectMissingClientExecutable()
        {
            var logger = new TestLogger();
            var service = new ClientLaunchService(logger);
            var missingPath = Path.Combine(
                TestContext.CurrentContext.WorkDirectory,
                $"{Guid.NewGuid():N}.exe");
            var options = new ClientLaunchOptions(
                new UserSettings
                {
                    ClientPath = missingPath
                });

            Assert.That(
                () => service.Launch(
                    options,
                    new ClientLayout()),
                Throws.TypeOf<FileNotFoundException>()
                    .With.Property("FileName")
                    .EqualTo(missingPath));
            Assert.That(
                logger.Errors,
                Has.Member(
                    "Client executable not found, unable to launch"));
        }

        [Test]
        public void ShouldVerifyAllSitesBeforeWritingLoginPatch()
        {
            using var stream = PatchStream();
            Write(
                stream,
                FirstAddress,
                0x75,
                0x6C);
            Write(
                stream,
                SecondAddress,
                0x00,
                0x00);
            Write(
                stream,
                ThirdAddress,
                0x68,
                0xE8,
                0x03,
                0x00,
                0x00);
            using var writer = new BinaryWriter(
                stream,
                Encoding.ASCII,
                leaveOpen: true);

            Assert.That(
                () => ClientLaunchService
                    .ApplySuppressLoginNotificationPatch(
                        stream,
                        writer),
                Throws.TypeOf<InvalidDataException>());
            Assert.That(
                Read(stream, FirstAddress, 2),
                Is.EqualTo(
                    new byte[] { 0x75, 0x6C }));
        }

        [Test]
        public void ShouldWriteVerifiedLoginPatch()
        {
            using var stream = PatchStream();
            Write(
                stream,
                FirstAddress,
                0x75,
                0x6C);
            Write(
                stream,
                SecondAddress,
                0x75,
                0x6D);
            Write(
                stream,
                ThirdAddress,
                0x68,
                0xE8,
                0x03,
                0x00,
                0x00);
            using var writer = new BinaryWriter(
                stream,
                Encoding.ASCII,
                leaveOpen: true);

            ClientLaunchService
                .ApplySuppressLoginNotificationPatch(
                    stream,
                    writer);

            Assert.Multiple(() =>
            {
                Assert.That(
                    Read(stream, FirstAddress, 2),
                    Is.EqualTo(
                        new byte[] { 0xEB, 0x6C }));
                Assert.That(
                    Read(stream, SecondAddress, 2),
                    Is.EqualTo(
                        new byte[] { 0xEB, 0x6D }));
                Assert.That(
                    Read(stream, ThirdAddress, 5),
                    Is.EqualTo(
                        new byte[]
                        {
                            0x68,
                            0x00,
                            0x00,
                            0x00,
                            0x00
                        }));
            });
        }

        private static MemoryStream PatchStream()
        {
            var stream = new MemoryStream();
            stream.SetLength(ThirdAddress + 5);
            return stream;
        }

        private static byte[] Read(
            Stream stream,
            long address,
            int length)
        {
            stream.Position = address;
            var bytes = new byte[length];
            stream.ReadExactly(bytes);
            return bytes;
        }

        private static void Write(
            Stream stream,
            long address,
            params byte[] bytes)
        {
            stream.Position = address;
            stream.Write(bytes);
        }
    }
}
