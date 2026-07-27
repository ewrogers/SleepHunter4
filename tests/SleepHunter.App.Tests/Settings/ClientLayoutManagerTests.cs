using System.Text;
using System.Xml;
using SleepHunter.Settings;

namespace SleepHunter.Tests.Settings
{
    public sealed class ClientLayoutManagerTests
    {
        [Test]
        public void ShouldLoadSingleLayoutAndLeaveStreamOpen()
        {
            using var stream = Stream(ValidLayout);
            var manager = new ClientLayoutManager();

            manager.LoadFromStream(stream);

            var layout = manager.Layout;
            Assert.Multiple(() =>
            {
                Assert.That(layout, Is.Not.Null);
                Assert.That(
                    layout.PointerWidth,
                    Is.EqualTo("Bit32"));
                Assert.That(
                    layout.WindowClassName,
                    Is.EqualTo("DarkAges"));
                Assert.That(stream.CanRead, Is.True);
            });
        }

        [Test]
        public void ShouldRetainAcceptedLayoutWhenReplacementIsInvalid()
        {
            using var valid = Stream(ValidLayout);
            var manager = new ClientLayoutManager();
            manager.LoadFromStream(valid);
            var accepted = manager.Layout;
            using var invalid = Stream(
                ValidLayout.Replace(
                    "PointerWidth=\"Bit32\"",
                    "PointerWidth=\"Bit64\"",
                    StringComparison.Ordinal));

            Assert.That(
                () => manager.LoadFromStream(invalid),
                Throws.TypeOf<InvalidDataException>());
            Assert.That(
                manager.Layout,
                Is.SameAs(accepted));
        }

        [Test]
        public void ShouldRejectMissingDetectionMetadata()
        {
            const string xml = """
                <ClientLayout PointerWidth="Bit32">
                  <Variables>
                    <Static Key="Value" Address="1000" Type="Byte" />
                  </Variables>
                </ClientLayout>
                """;
            using var stream = Stream(xml);
            var manager = new ClientLayoutManager();

            Assert.That(
                () => manager.LoadFromStream(stream),
                Throws.TypeOf<InvalidDataException>());
        }

        [Test]
        public void ShouldRejectInvalidMemoryMappingBeforeReplacingLayout()
        {
            using var valid = Stream(ValidLayout);
            var manager = new ClientLayoutManager();
            manager.LoadFromStream(valid);
            var accepted = manager.Layout;
            using var invalid = Stream(
                ValidLayout.Replace(
                    "Address=\"1000\"",
                    "Address=\"0\"",
                    StringComparison.Ordinal));

            Assert.That(
                () => manager.LoadFromStream(invalid),
                Throws.TypeOf<InvalidDataException>());
            Assert.That(
                manager.Layout,
                Is.SameAs(accepted));
        }

        [Test]
        public void ShouldRejectOversizedLayout()
        {
            using var stream = new MemoryStream(
                new byte[1_048_577]);
            var manager = new ClientLayoutManager();

            Assert.That(
                () => manager.LoadFromStream(stream),
                Throws.TypeOf<InvalidDataException>());
        }

        [Test]
        public void ShouldProhibitDocumentTypeDefinitions()
        {
            const string xml = """
                <!DOCTYPE ClientLayout [
                  <!ENTITY className "DarkAges">
                ]>
                <ClientLayout PointerWidth="Bit32">
                  <WindowClassName>&className;</WindowClassName>
                  <Variables>
                    <Static Key="Value" Address="1000" Type="Byte" />
                  </Variables>
                </ClientLayout>
                """;
            using var stream = Stream(xml);
            var manager = new ClientLayoutManager();

            Assert.That(
                () => manager.LoadFromStream(stream),
                Throws.TypeOf<XmlException>());
        }

        private const string ValidLayout = """
            <ClientLayout PointerWidth="Bit32">
              <ExecutableName>Darkages.exe</ExecutableName>
              <WindowClassName>DarkAges</WindowClassName>
              <WindowTitle>Darkages</WindowTitle>
              <Variables>
                <Static Key="Value" Address="1000" Type="Byte" />
              </Variables>
            </ClientLayout>
            """;

        private static MemoryStream Stream(string value) =>
            new(Encoding.UTF8.GetBytes(value));
    }
}
