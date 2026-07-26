using SleepHunter.Interop.Mappings;
using SleepHunter.Interop.Memory;

namespace SleepHunter.Interop.Tests.Mappings;

public sealed class ProductionClientMemoryMapTests
{
    private ClientMemoryMap map = null!;

    [OneTimeSetUp]
    public void LoadMap()
    {
        var path = Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "Data",
            "ClientLayout.xml");
        using var stream = File.OpenRead(path);
        map = ClientMemoryMapLoader.Load(stream);
    }

    [Test]
    public void ShouldUseTheDocumentedCharacterAndVitalsRoots()
    {
        var currentHealth = Required("CurrentHealth");
        var characterClass = Required("CharacterClass");
        var displayClass = Required("DisplayClass");

        Assert.Multiple(() =>
        {
            Assert.That(
                currentHealth.Address.BaseAddress,
                Is.EqualTo(new MemoryAddress(0x73D964)));
            Assert.That(
                currentHealth.Address.Offsets.Select(
                    offset => offset.Value),
                Is.EqualTo(new long[] { -0x20, 0x1078 }));
            Assert.That(
                currentHealth.ValueKind,
                Is.EqualTo(MemoryValueKind.Unsigned32));
            Assert.That(
                characterClass.Address.Offsets.Select(
                    offset => offset.Value),
                Is.EqualTo(new long[] { -0x20, 0x1089 }));
            Assert.That(
                characterClass.ValueKind,
                Is.EqualTo(MemoryValueKind.Byte));
            Assert.That(
                displayClass.Address.BaseAddress,
                Is.EqualTo(new MemoryAddress(0x6FC914)));
            Assert.That(displayClass.MaximumLength, Is.EqualTo(128));
            Assert.That(
                displayClass.Address.Offsets.Select(
                    offset => offset.Value),
                Is.EqualTo(new long[] { 0xBDC }));
        });
    }

    [Test]
    public void ShouldUseTheCoherentPaneSnapshotRoots()
    {
        var equipment = Required("EquipmentSnapshot");
        var inventory = Required("InventoryPanes");
        var skillbook = Required("SkillbookPanes");
        var spellbook = Required("SpellbookPanes");

        Assert.Multiple(() =>
        {
            Assert.That(equipment.Capacity, Is.EqualTo(18));
            Assert.That(
                equipment.Address.Offsets.Select(
                    offset => offset.Value),
                Is.EqualTo(new long[] { 0x111C }));
            Assert.That(inventory.Capacity, Is.EqualTo(60));
            Assert.That(
                inventory.Address.Offsets.Select(
                    offset => offset.Value),
                Is.EqualTo(new long[] { 0x4DF8, 0x1A0 }));
            Assert.That(skillbook.Capacity, Is.EqualTo(90));
            Assert.That(
                skillbook.Address.Offsets.Select(
                    offset => offset.Value),
                Is.EqualTo(
                    new long[] { 0x4DFC, 0x224, 0x194, 0 }));
            Assert.That(spellbook.Capacity, Is.EqualTo(90));
            Assert.That(
                spellbook.Address.Offsets.Select(
                    offset => offset.Value),
                Is.EqualTo(
                    new long[] { 0x4DFC, 0x228, 0x194, 0 }));
        });
    }

    [Test]
    public void ShouldRetainBoundedCompactBookFallbacks()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                Required("Skillbook").Capacity,
                Is.EqualTo(89));
            Assert.That(
                Required("Spellbook").Capacity,
                Is.EqualTo(89));
        });
    }

    [Test]
    public void ShouldExposeWorldDialogAndEffectSources()
    {
        var worldUser = Required("WorldUserFunc");
        var worldObjects = Required("WorldObjectList");
        var groupCache = Required("GroupMemberCache");
        var effects = Required("ActiveSpellEffects");

        Assert.Multiple(() =>
        {
            Assert.That(
                worldUser.Address.Offsets.Select(
                    offset => offset.Value),
                Is.EqualTo(new long[] { -0x20, 0 }));
            Assert.That(
                worldObjects.Address.Offsets.Select(
                    offset => offset.Value),
                Is.EqualTo(new long[] { -0x158, 0 }));
            Assert.That(groupCache.RecordSize, Is.EqualTo(0x41));
            Assert.That(groupCache.Capacity, Is.EqualTo(64));
            Assert.That(effects.RecordSize, Is.EqualTo(30));
            Assert.That(effects.Capacity, Is.EqualTo(10));
        });
    }

    private MemoryVariableDefinition Required(string key) =>
        map.Find(key) ??
        throw new AssertionException(
            $"The production client map is missing '{key}'.");
}
