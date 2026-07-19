using System.Buffers.Binary;
using System.Text;
using SleepHunter.Converters;
using SleepHunter.Models;

namespace SleepHunter.Tests.Models
{
    [TestFixture]
    public sealed class EquipmentSetTests
    {
        [Test]
        public void ShouldParseEquipmentSpriteDyeNameAndDurabilityFromOneSnapshot()
        {
            const int slotIndex = (int)EquipmentSlot.Hat;
            var snapshot = new byte[EquipmentSet.EquipmentSnapshotSize];
            BinaryPrimitives.WriteUInt16LittleEndian(snapshot.AsSpan(slotIndex * 2), 0x8D24);
            snapshot[0x24 + slotIndex] = 1;
            Encoding.ASCII.GetBytes("Winter Scarf 2").CopyTo(snapshot.AsSpan(0x36 + slotIndex * 128));
            BinaryPrimitives.WriteUInt32LittleEndian(snapshot.AsSpan(0x938 + slotIndex * 8), 29976);
            BinaryPrimitives.WriteUInt32LittleEndian(snapshot.AsSpan(0x93C + slotIndex * 8), 30000);

            var records = EquipmentSet.ParseEquipmentSnapshot(snapshot, EquipmentSet.EquipmentCount);
            var record = records[slotIndex];

            Assert.Multiple(() =>
            {
                Assert.That(record.IsPresent, Is.True);
                Assert.That(record.RawSprite, Is.EqualTo(0x8D24));
                Assert.That(record.DyeColor, Is.EqualTo(1));
                Assert.That(record.Name, Is.EqualTo("Winter Scarf 2"));
                Assert.That(record.Durability, Is.EqualTo(29976));
                Assert.That(record.MaximumDurability, Is.EqualTo(30000));
            });
        }

        [Test]
        public void ShouldPlaceEquipmentLikeTheClientPane()
        {
            var armor = PositionOf(EquipmentSlot.Armor);
            var weapon = PositionOf(EquipmentSlot.Weapon);
            var overcoat = PositionOf(EquipmentSlot.Overcoat);
            var helmet = PositionOf(EquipmentSlot.Helmet);
            var overhelm = PositionOf(EquipmentSlot.Hat);
            var shield = PositionOf(EquipmentSlot.Shield);
            var accessory1 = PositionOf(EquipmentSlot.Accessory1);
            var accessory2 = PositionOf(EquipmentSlot.Accessory2);
            var accessory3 = PositionOf(EquipmentSlot.Accessory3);

            Assert.Multiple(() =>
            {
                Assert.That(overcoat, Is.EqualTo((armor.Row, armor.Column - 1)));
                Assert.That(weapon, Is.EqualTo((armor.Row + 1, armor.Column)));
                Assert.That(overhelm, Is.EqualTo((helmet.Row + 1, helmet.Column)));
                Assert.That(accessory1, Is.EqualTo((shield.Row - 1, shield.Column)));
                Assert.That(accessory2, Is.EqualTo((shield.Row - 1, shield.Column + 1)));
                Assert.That(accessory3, Is.EqualTo((shield.Row, shield.Column + 1)));
            });
        }

        private static (int Row, int Column) PositionOf(EquipmentSlot slot) =>
            EquipmentSlotPositionConverter.GetPosition((int)slot + 1);
    }
}
