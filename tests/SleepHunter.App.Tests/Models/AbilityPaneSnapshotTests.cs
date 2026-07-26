using System.Buffers.Binary;
using System.Text;

using SleepHunter.Models;

namespace SleepHunter.Tests.Models
{
    [TestFixture]
    public sealed class AbilityPaneSnapshotTests
    {
        [Test]
        public void ShouldParseTheDocumentedSkillPaneFields()
        {
            var snapshot = new byte[0x1B8];
            BinaryPrimitives.WriteUInt16LittleEndian(snapshot.AsSpan(0x00, 2), 321);
            Encoding.ASCII.GetBytes("Assail (Lev:3/100)\0").CopyTo(snapshot.AsSpan(0x02));
            snapshot[0x182] = 37;
            BinaryPrimitives.WriteUInt32LittleEndian(snapshot.AsSpan(0x184, 4), 12);
            BinaryPrimitives.WriteUInt32LittleEndian(snapshot.AsSpan(0x188, 4), 1000);
            BinaryPrimitives.WriteUInt32LittleEndian(snapshot.AsSpan(0x18C, 4), 2000);
            snapshot[0x190] = 1;
            snapshot[0x192] = 1;
            BinaryPrimitives.WriteInt32LittleEndian(snapshot.AsSpan(0x1AC, 4), 3);
            BinaryPrimitives.WriteInt32LittleEndian(snapshot.AsSpan(0x1B0, 4), 100);
            BinaryPrimitives.WriteInt32LittleEndian(snapshot.AsSpan(0x1B4, 4), 6);

            var record = Skillbook.ParseSkillPaneSnapshot(snapshot);

            Assert.Multiple(() =>
            {
                Assert.That(record.IconIndex, Is.EqualTo(321));
                Assert.That(record.Name, Is.EqualTo("Assail (Lev:3/100)"));
                Assert.That(record.Slot, Is.EqualTo(37));
                Assert.That(record.CooldownProgress, Is.EqualTo(12));
                Assert.That(record.CooldownStartMilliseconds, Is.EqualTo(1000));
                Assert.That(record.CooldownEndMilliseconds, Is.EqualTo(2000));
                Assert.That(record.CooldownVisualActive, Is.True);
                Assert.That(record.ActionDelayActive, Is.True);
                Assert.That(record.NameSuffixLeft, Is.EqualTo(3));
                Assert.That(record.NameSuffixRight, Is.EqualTo(100));
                Assert.That(record.BaseNameLength, Is.EqualTo(6));
            });
        }

        [Test]
        public void ShouldParseTheDocumentedSpellPaneFields()
        {
            var snapshot = new byte[0x12C];
            snapshot[0x00] = 73;
            BinaryPrimitives.WriteUInt16LittleEndian(snapshot.AsSpan(0x02, 2), 456);
            snapshot[0x04] = (byte)AbilityTargetType.Target;
            Encoding.ASCII.GetBytes("ard cradh\0").CopyTo(snapshot.AsSpan(0x05));
            Encoding.ASCII.GetBytes("Who?\0").CopyTo(snapshot.AsSpan(0x85));
            snapshot[0x105] = 4;
            snapshot[0x107] = 1;
            BinaryPrimitives.WriteInt32LittleEndian(snapshot.AsSpan(0x120, 4), 7);
            BinaryPrimitives.WriteInt32LittleEndian(snapshot.AsSpan(0x124, 4), 8);
            BinaryPrimitives.WriteInt32LittleEndian(snapshot.AsSpan(0x128, 4), 9);

            var record = Spellbook.ParseSpellPaneSnapshot(snapshot);

            Assert.Multiple(() =>
            {
                Assert.That(record.Slot, Is.EqualTo(73));
                Assert.That(record.IconIndex, Is.EqualTo(456));
                Assert.That(record.TargetType, Is.EqualTo(AbilityTargetType.Target));
                Assert.That(record.Name, Is.EqualTo("ard cradh"));
                Assert.That(record.Prompt, Is.EqualTo("Who?"));
                Assert.That(record.CastLines, Is.EqualTo(4));
                Assert.That(record.ActionDelayActive, Is.True);
                Assert.That(record.NameSuffixLeft, Is.EqualTo(7));
                Assert.That(record.NameSuffixRight, Is.EqualTo(8));
                Assert.That(record.BaseNameLength, Is.EqualTo(9));
            });
        }

        [TestCase(1, 1)]
        [TestCase(36, 36)]
        [TestCase(37, 1)]
        [TestCase(72, 36)]
        [TestCase(73, 1)]
        [TestCase(90, 18)]
        public void ShouldDisplayOneBasedSlotsWithinEachAbilityPane(
            int slot,
            int expectedRelativeSlot)
        {
            var ability = new Skill { Slot = slot };

            Assert.That(
                ability.RelativeSlot,
                Is.EqualTo(expectedRelativeSlot));
        }

        [Test]
        public void ShouldCalculateSkillCooldownAcrossTickCountWraparound()
        {
            var skill = new Skill
            {
                IsOnCooldown = true,
                CooldownProgress = 15,
                CooldownStartMilliseconds = uint.MaxValue - 99,
                CooldownEndMilliseconds = 100
            };

            Assert.Multiple(() =>
            {
                Assert.That(skill.CooldownProgressPercent, Is.EqualTo(0.5));
                Assert.That(skill.CooldownRemainingFraction, Is.EqualTo(0.5));
                Assert.That(skill.CooldownDurationMilliseconds, Is.EqualTo(200));
                Assert.That(skill.GetRemainingCooldownMilliseconds(0), Is.EqualTo(100));
            });
        }

        [Test]
        public void ShouldNotTreatRetainedSkillProgressAsAnActiveCooldown()
        {
            var snapshot = new byte[0x1B8];
            snapshot[0x182] = 1;
            BinaryPrimitives.WriteUInt32LittleEndian(snapshot.AsSpan(0x184, 4), 29);
            snapshot[0x190] = 0;

            var record = Skillbook.ParseSkillPaneSnapshot(snapshot);

            Assert.Multiple(() =>
            {
                Assert.That(record.CooldownProgress, Is.EqualTo(29));
                Assert.That(record.CooldownVisualActive, Is.False);
                Assert.That(record.IsCooldownActive, Is.False);
            });
        }

        [Test]
        public void ShouldUseTheSkillVisualFlagAtTheStartOfCooldown()
        {
            var snapshot = new byte[0x1B8];
            snapshot[0x182] = 1;
            BinaryPrimitives.WriteUInt32LittleEndian(snapshot.AsSpan(0x184, 4), 0);
            snapshot[0x190] = 1;

            var record = Skillbook.ParseSkillPaneSnapshot(snapshot);

            Assert.That(record.IsCooldownActive, Is.True);
        }

        [TestCase(0u, 1.0)]
        [TestCase(1u, 29.0 / 30.0)]
        [TestCase(15u, 0.5)]
        [TestCase(29u, 1.0 / 30.0)]
        [TestCase(30u, 0.0)]
        [TestCase(31u, 0.0)]
        public void ShouldConvertCooldownStepsIntoARemainingOverlay(
            uint progress,
            double expectedRemainingFraction)
        {
            var skill = new Skill { CooldownProgress = progress };

            Assert.That(
                skill.CooldownRemainingFraction,
                Is.EqualTo(expectedRemainingFraction).Within(0.000001));
        }
    }
}
