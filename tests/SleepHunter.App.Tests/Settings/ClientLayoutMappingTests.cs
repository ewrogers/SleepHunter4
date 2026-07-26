using SleepHunter.IO.Process;
using SleepHunter.Settings;

namespace SleepHunter.Tests.Settings
{
    [TestFixture]
    public sealed class ClientLayoutMappingTests
    {
        private ClientLayout layout = null!;

        [OneTimeSetUp]
        public void LoadLayout()
        {
            ClientLayoutManager.Instance.LoadFromFile(
                FindLayoutFile());
            layout = ClientLayoutManager.Instance.Layout;
        }

        [Test]
        public void ShouldReadVitalsFromTheDocumentedWorldUserRoot()
        {
            var currentHealth =
                (DynamicMemoryVariable)layout.GetVariable(
                    "CurrentHealth");

            Assert.Multiple(() =>
            {
                Assert.That(currentHealth.Address, Is.EqualTo(0x73D964));
                Assert.That(currentHealth.ValueType, Is.EqualTo(MemoryValueType.UInt32));
                Assert.That(currentHealth.Offsets, Has.Count.EqualTo(2));
                Assert.That(currentHealth.Offsets[0].Offset, Is.EqualTo(0x20));
                Assert.That(currentHealth.Offsets[0].IsNegative, Is.True);
                Assert.That(currentHealth.Offsets[1].Offset, Is.EqualTo(0x1078));
            });
        }

        [Test]
        public void ShouldExposeBaseAndDisplayClassSources()
        {
            var characterClass =
                (DynamicMemoryVariable)layout.GetVariable(
                    "CharacterClass");
            var displayClass =
                (DynamicMemoryVariable)layout.GetVariable(
                    "DisplayClass");

            Assert.Multiple(() =>
            {
                Assert.That(characterClass.ValueType, Is.EqualTo(MemoryValueType.Byte));
                Assert.That(characterClass.Offsets[1].Offset, Is.EqualTo(0x1089));
                Assert.That(displayClass.Address, Is.EqualTo(0x6FC914));
                Assert.That(displayClass.MaxLength, Is.EqualTo(128));
                Assert.That(displayClass.Offsets.Single().Offset, Is.EqualTo(0xBDC));
            });
        }

        [Test]
        public void ShouldUseTheExecutableVerifiedEquipmentPaneRoot()
        {
            var equipment =
                (DynamicMemoryVariable)layout.GetVariable(
                    "EquipmentSnapshot");
            var equipPaneVariables = new[]
            {
                "Nation",
                "Title",
                "DisplayClass",
                "Guild",
                "GuildRank",
                "GroupMembers",
                "Equipment",
                "EquipmentSnapshot"
            };

            Assert.Multiple(() =>
            {
                Assert.That(equipment.Address, Is.EqualTo(0x6FC914));
                Assert.That(equipment.Offsets.Single().Offset, Is.EqualTo(0x111C));
                Assert.That(equipment.Count, Is.EqualTo(18));
                Assert.That(
                    equipPaneVariables.Select(
                        key => layout.GetVariable(key).Address),
                    Is.All.EqualTo(0x6FC914));
            });
        }

        [Test]
        public void ShouldExposePaneOwnedProgressionAndCombatStats()
        {
            var totalAbility =
                (DynamicMemoryVariable)layout.GetVariable(
                    "TotalAbility");
            var armorClass =
                (DynamicMemoryVariable)layout.GetVariable(
                    "ArmorClass");

            Assert.Multiple(() =>
            {
                Assert.That(totalAbility.Address, Is.EqualTo(0x82B768));
                Assert.That(totalAbility.Offsets.Select(offset => offset.Offset), Is.EqualTo(new long[] { 0x4E10, 0x1E0 }));
                Assert.That(armorClass.ValueType, Is.EqualTo(MemoryValueType.SByte));
                Assert.That(armorClass.Offsets.Select(offset => offset.Offset), Is.EqualTo(new long[] { 0x4E14, 0x4F8 }));
            });
        }

        [Test]
        public void ShouldNotReadPastTheCompactSessionBooks()
        {
            Assert.Multiple(() =>
            {
                Assert.That(
                    layout.GetVariable("Skillbook").Count,
                    Is.EqualTo(89));
                Assert.That(
                    layout.GetVariable("Spellbook").Count,
                    Is.EqualTo(89));
            });
        }

        [Test]
        public void ShouldExposePaneBackedBooksWithoutRemovingCompactFallbacks()
        {
            var skillPanes =
                (DynamicMemoryVariable)layout.GetVariable(
                    "SkillbookPanes");
            var spellPanes =
                (DynamicMemoryVariable)layout.GetVariable(
                    "SpellbookPanes");
            var skillCapacity =
                (DynamicMemoryVariable)layout.GetVariable(
                    "SkillbookPaneCapacity");
            var spellCapacity =
                (DynamicMemoryVariable)layout.GetVariable(
                    "SpellbookPaneCapacity");

            Assert.Multiple(() =>
            {
                Assert.That(skillPanes.Count, Is.EqualTo(90));
                Assert.That(skillPanes.Offsets.Select(offset => offset.Offset),
                    Is.EqualTo(new long[] { 0x4DFC, 0x224, 0x194, 0 }));
                Assert.That(spellPanes.Count, Is.EqualTo(90));
                Assert.That(spellPanes.Offsets.Select(offset => offset.Offset),
                    Is.EqualTo(new long[] { 0x4DFC, 0x228, 0x194, 0 }));
                Assert.That(skillCapacity.Offsets.Select(offset => offset.Offset),
                    Is.EqualTo(new long[] { 0x4DFC, 0x224, 0x190 }));
                Assert.That(spellCapacity.Offsets.Select(offset => offset.Offset),
                    Is.EqualTo(new long[] { 0x4DFC, 0x228, 0x190 }));
                Assert.That(
                    layout.ContainsVariable("Skillbook"),
                    Is.True);
                Assert.That(
                    layout.ContainsVariable("Spellbook"),
                    Is.True);
            });
        }

        [Test]
        public void ShouldExposeWorldGroupAndEventDispatcherRoots()
        {
            var worldUser =
                (DynamicMemoryVariable)layout.GetVariable(
                    "WorldUserFunc");
            var worldObjects =
                (DynamicMemoryVariable)layout.GetVariable(
                    "WorldObjectList");
            var groupCache =
                (DynamicMemoryVariable)layout.GetVariable(
                    "GroupMemberCache");
            var eventEntries =
                (DynamicMemoryVariable)layout.GetVariable(
                    "EventPaneEntries");

            Assert.Multiple(() =>
            {
                Assert.That(worldUser.Offsets.Select(offset => offset.Offset),
                    Is.EqualTo(new long[] { 0x20, 0 }));
                Assert.That(worldObjects.Offsets.Select(offset => offset.Offset),
                    Is.EqualTo(new long[] { 0x158, 0 }));
                Assert.That(groupCache.Size, Is.EqualTo(0x41));
                Assert.That(groupCache.Count, Is.EqualTo(64));
                Assert.That(eventEntries.Address, Is.EqualTo(0x6D9220));
                Assert.That(eventEntries.Offsets.Select(offset => offset.Offset),
                    Is.EqualTo(new long[] { 0x64, 0 }));
            });
        }

        private static string FindLayoutFile()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null)
            {
                var candidate = Path.Combine(
                    directory.FullName,
                    "data",
                    "ClientLayout.xml");
                if (File.Exists(candidate))
                    return candidate;

                directory = directory.Parent;
            }

            throw new FileNotFoundException(
                "Could not locate data/ClientLayout.xml from the test directory.");
        }
    }
}
