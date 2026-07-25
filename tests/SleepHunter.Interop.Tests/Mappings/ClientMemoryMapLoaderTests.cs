using System.Text;
using SleepHunter.Interop.Mappings;
using SleepHunter.Interop.Memory;

namespace SleepHunter.Interop.Tests.Mappings;

public sealed class ClientMemoryMapLoaderTests
{
    [Test]
    public void ShouldLoadExactUsdaMappingFromRuntimeConfiguration()
    {
        var path = Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "Data",
            "Versions.xml");
        using var stream = File.OpenRead(path);

        var map = ClientMemoryMapLoader.Load(stream, "usda 7.41");

        var characterName = map.Find("CharacterName");
        var characterId = map.Find("CharacterId");
        var inventory = map.Find("Inventory");
        var cooldowns = map.Find("SkillCooldowns");
        var activePanel = map.Find("ActivePanel");
        Assert.Multiple(() =>
        {
            Assert.That(map.VersionKey, Is.EqualTo("USDA 7.41"));
            Assert.That(map.PointerWidth, Is.EqualTo(PointerWidth.Bit32));
            Assert.That(characterName?.ValueKind, Is.EqualTo(MemoryValueKind.Text));
            Assert.That(characterName?.MaximumLength, Is.EqualTo(16));
            Assert.That(characterName?.Address.IsStatic, Is.True);
            Assert.That(characterId?.ValueKind, Is.EqualTo(MemoryValueKind.Unsigned32));
            Assert.That(
                characterId?.Address.Offsets.Select(offset => offset.Value),
                Is.EqualTo(new long[] { -0x20, 0x1050 }));
            Assert.That(inventory?.ValueKind, Is.EqualTo(MemoryValueKind.Binary));
            Assert.That(inventory?.RecordSize, Is.EqualTo(262));
            Assert.That(inventory?.Capacity, Is.EqualTo(60));
            Assert.That(cooldowns?.RequiresSearch, Is.True);
            Assert.That(cooldowns?.Search?.MatchOffset.Value, Is.EqualTo(0x194));
            Assert.That(activePanel?.ValueKind, Is.EqualTo(MemoryValueKind.Byte));
        });
    }

    [Test]
    public void ShouldSelectOnlyTheRequestedVersionAndLeaveStreamOpen()
    {
        const string xml = """
            <ClientVersions>
              <Clients>
                <Client Key="Valid" PointerWidth="Bit32">
                  <Variables>
                    <Static Key="Value" Address="1000" Type="UInt32" />
                  </Variables>
                </Client>
                <Client Key="Unrelated" PointerWidth="Bit32">
                  <Variables>
                    <Static Key="Ambiguous" Address="2000" />
                  </Variables>
                </Client>
              </Clients>
            </ClientVersions>
            """;
        using var stream = Stream(xml);

        var map = ClientMemoryMapLoader.Load(stream, "VALID");

        Assert.Multiple(() =>
        {
            Assert.That(map.Find("Value")?.ValueKind, Is.EqualTo(MemoryValueKind.Unsigned32));
            Assert.That(stream.CanRead, Is.True);
        });
    }

    [Test]
    public void ShouldRejectMissingDuplicateAndAmbiguousMappings()
    {
        const string duplicate = """
            <ClientVersions>
              <Clients>
                <Client Key="Same" PointerWidth="Bit32"><Variables /></Client>
                <Client Key="same" PointerWidth="Bit32"><Variables /></Client>
              </Clients>
            </ClientVersions>
            """;
        const string ambiguous = """
            <ClientVersions>
              <Clients>
                <Client Key="Version" PointerWidth="Bit32">
                  <Variables>
                    <Static Key="Value" Address="1000" />
                  </Variables>
                </Client>
              </Clients>
            </ClientVersions>
            """;

        Assert.Multiple(() =>
        {
            using var missingStream = Stream(duplicate);
            Assert.Throws<InvalidDataException>(
                () => ClientMemoryMapLoader.Load(missingStream, "Missing"));

            using var duplicateStream = Stream(duplicate);
            Assert.Throws<InvalidDataException>(
                () => ClientMemoryMapLoader.Load(duplicateStream, "Same"));

            using var ambiguousStream = Stream(ambiguous);
            Assert.Throws<InvalidDataException>(
                () => ClientMemoryMapLoader.Load(ambiguousStream, "Version"));
        });
    }

    [Test]
    public void ShouldRejectInvalidWidthsTypesOffsetsAndLimits()
    {
        const string invalidWidth = """
            <ClientVersions>
              <Clients>
                <Client Key="Version" PointerWidth="Bit16"><Variables /></Client>
              </Clients>
            </ClientVersions>
            """;
        const string invalidType = """
            <ClientVersions>
              <Clients>
                <Client Key="Version" PointerWidth="Bit32">
                  <Variables>
                    <Static Key="Value" Address="1000" Type="Float32" />
                  </Variables>
                </Client>
              </Clients>
            </ClientVersions>
            """;
        const string invalidOffset = """
            <ClientVersions>
              <Clients>
                <Client Key="Version" PointerWidth="Bit32">
                  <Variables>
                    <Dynamic Key="Value" Address="1000" Type="Byte">
                      <Offsets><Offset Value="not-hex" /></Offsets>
                    </Dynamic>
                  </Variables>
                </Client>
              </Clients>
            </ClientVersions>
            """;
        const string tooManyOffsets = """
            <ClientVersions>
              <Clients>
                <Client Key="Version" PointerWidth="Bit32">
                  <Variables>
                    <Dynamic Key="Value" Address="1000" Type="Byte">
                      <Offsets>
                        <Offset Value="1" />
                        <Offset Value="2" />
                      </Offsets>
                    </Dynamic>
                  </Variables>
                </Client>
              </Clients>
            </ClientVersions>
            """;

        Assert.Multiple(() =>
        {
            using var widthStream = Stream(invalidWidth);
            Assert.Throws<InvalidDataException>(
                () => ClientMemoryMapLoader.Load(widthStream, "Version"));

            using var typeStream = Stream(invalidType);
            Assert.Throws<InvalidDataException>(
                () => ClientMemoryMapLoader.Load(typeStream, "Version"));

            using var offsetStream = Stream(invalidOffset);
            Assert.Throws<InvalidDataException>(
                () => ClientMemoryMapLoader.Load(offsetStream, "Version"));

            using var limitedStream = Stream(tooManyOffsets);
            var limits = new ClientMemoryMapLoadLimits(
                maximumOffsetsPerVariable: 1);
            Assert.Throws<InvalidDataException>(
                () => ClientMemoryMapLoader.Load(
                    limitedStream,
                    "Version",
                    limits));
        });
    }

    [Test]
    public void ShouldProhibitDocumentTypeDefinitions()
    {
        const string xml = """
            <!DOCTYPE ClientVersions [
              <!ENTITY value "1000">
            ]>
            <ClientVersions>
              <Clients>
                <Client Key="Version" PointerWidth="Bit32">
                  <Variables>
                    <Static Key="Value" Address="&value;" Type="Byte" />
                  </Variables>
                </Client>
              </Clients>
            </ClientVersions>
            """;
        using var stream = Stream(xml);

        Assert.Throws<System.Xml.XmlException>(
            () => ClientMemoryMapLoader.Load(stream, "Version"));
    }

    private static MemoryStream Stream(string value) =>
        new(Encoding.UTF8.GetBytes(value));
}
