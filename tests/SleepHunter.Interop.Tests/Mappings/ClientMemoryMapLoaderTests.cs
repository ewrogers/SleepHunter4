using System.Text;
using SleepHunter.Interop.Mappings;
using SleepHunter.Interop.Memory;

namespace SleepHunter.Interop.Tests.Mappings;

public sealed class ClientMemoryMapLoaderTests
{
    [Test]
    public void ShouldLoadUnifiedMappingFromRuntimeConfiguration()
    {
        var path = Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "Data",
            "ClientLayout.xml");
        using var stream = File.OpenRead(path);

        var map = ClientMemoryMapLoader.Load(stream);

        var characterName = map.Find("CharacterName");
        var characterId = map.Find("CharacterId");
        var inventory = map.Find("Inventory");
        var cooldowns = map.Find("SkillCooldowns");
        var activePanel = map.Find("ActivePanel");
        var minimizedMode = map.Find("MinimizedMode");
        var mapName = map.Find("MapName");
        var eventDispatcher = map.Find("ActiveEventDispatcher");
        var dialogVtable = map.Find("WindowMessageDialogPaneVtable");
        Assert.Multiple(() =>
        {
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
            Assert.That(
                minimizedMode?.Address.Offsets.Select(offset => offset.Value),
                Is.EqualTo(new long[] { 0x4C60 }));
            Assert.That(mapName?.ValueKind, Is.EqualTo(MemoryValueKind.Text));
            Assert.That(
                mapName?.Address.Offsets.Select(offset => offset.Value),
                Is.EqualTo(new long[] { 0x4CAC }));
            Assert.That(
                eventDispatcher?.Address.BaseAddress,
                Is.EqualTo(new MemoryAddress(0x73D944)));
            Assert.That(
                eventDispatcher?.Address.Offsets.Select(offset => offset.Value),
                Is.EqualTo(new long[] { 0 }));
            Assert.That(
                dialogVtable?.Address.BaseAddress,
                Is.EqualTo(new MemoryAddress(0x672A84)));
            Assert.That(dialogVtable?.Address.IsStatic, Is.True);
        });
    }

    [Test]
    public void ShouldLoadTheSingleMappingAndLeaveStreamOpen()
    {
        const string xml = """
            <ClientLayout PointerWidth="Bit32">
              <Variables>
                <Static Key="Value" Address="1000" Type="UInt32" />
              </Variables>
            </ClientLayout>
            """;
        using var stream = Stream(xml);

        var map = ClientMemoryMapLoader.Load(stream);

        Assert.Multiple(() =>
        {
            Assert.That(map.Find("Value")?.ValueKind, Is.EqualTo(MemoryValueKind.Unsigned32));
            Assert.That(stream.CanRead, Is.True);
        });
    }

    [Test]
    public void ShouldRejectLegacyCollectionAndMissingOrEmptyVariables()
    {
        const string missingVariables = """
            <ClientLayout PointerWidth="Bit32" />
            """;
        const string emptyVariables = """
            <ClientLayout PointerWidth="Bit32">
              <Variables />
            </ClientLayout>
            """;
        const string legacyCollection = """
            <ClientVersions>
              <Clients>
                <Client PointerWidth="Bit32"><Variables /></Client>
              </Clients>
            </ClientVersions>
            """;

        Assert.Multiple(() =>
        {
            using var missingStream = Stream(missingVariables);
            Assert.Throws<InvalidDataException>(
                () => ClientMemoryMapLoader.Load(missingStream));

            using var emptyStream = Stream(emptyVariables);
            Assert.Throws<InvalidDataException>(
                () => ClientMemoryMapLoader.Load(emptyStream));

            using var legacyStream = Stream(legacyCollection);
            Assert.Throws<InvalidDataException>(
                () => ClientMemoryMapLoader.Load(legacyStream));
        });
    }

    [Test]
    public void ShouldRejectInvalidWidthsTypesOffsetsAndLimits()
    {
        const string invalidWidth = """
            <ClientLayout PointerWidth="Bit16"><Variables /></ClientLayout>
            """;
        const string invalidType = """
            <ClientLayout PointerWidth="Bit32">
              <Variables>
                <Static Key="Value" Address="1000" Type="Float32" />
              </Variables>
            </ClientLayout>
            """;
        const string invalidOffset = """
            <ClientLayout PointerWidth="Bit32">
              <Variables>
                <Dynamic Key="Value" Address="1000" Type="Byte">
                  <Offsets><Offset Value="not-hex" /></Offsets>
                </Dynamic>
              </Variables>
            </ClientLayout>
            """;
        const string tooManyOffsets = """
            <ClientLayout PointerWidth="Bit32">
              <Variables>
                <Dynamic Key="Value" Address="1000" Type="Byte">
                  <Offsets>
                    <Offset Value="1" />
                    <Offset Value="2" />
                  </Offsets>
                </Dynamic>
              </Variables>
            </ClientLayout>
            """;

        Assert.Multiple(() =>
        {
            using var widthStream = Stream(invalidWidth);
            Assert.Throws<InvalidDataException>(
                () => ClientMemoryMapLoader.Load(widthStream));

            using var typeStream = Stream(invalidType);
            Assert.Throws<InvalidDataException>(
                () => ClientMemoryMapLoader.Load(typeStream));

            using var offsetStream = Stream(invalidOffset);
            Assert.Throws<InvalidDataException>(
                () => ClientMemoryMapLoader.Load(offsetStream));

            using var limitedStream = Stream(tooManyOffsets);
            var limits = new ClientMemoryMapLoadLimits(
                maximumOffsetsPerVariable: 1);
            Assert.Throws<InvalidDataException>(
                () => ClientMemoryMapLoader.Load(
                    limitedStream,
                    limits));
        });
    }

    [Test]
    public void ShouldProhibitDocumentTypeDefinitions()
    {
        const string xml = """
            <!DOCTYPE ClientLayout [
              <!ENTITY value "1000">
            ]>
            <ClientLayout PointerWidth="Bit32">
              <Variables>
                <Static Key="Value" Address="&value;" Type="Byte" />
              </Variables>
            </ClientLayout>
            """;
        using var stream = Stream(xml);

        Assert.Throws<System.Xml.XmlException>(
            () => ClientMemoryMapLoader.Load(stream));
    }

    private static MemoryStream Stream(string value) =>
        new(Encoding.UTF8.GetBytes(value));
}
