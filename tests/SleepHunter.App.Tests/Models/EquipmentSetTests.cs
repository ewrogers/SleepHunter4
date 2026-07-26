using SleepHunter.Converters;
using SleepHunter.Models;

namespace SleepHunter.Tests.Models
{
    [TestFixture]
    public sealed class EquipmentSetTests
    {
        [Test]
        public void ShouldPlaceEquipmentLikeTheClientPane()
        {
            var armor = PositionOf(EquipmentSlot.Armor);
            var weapon = PositionOf(EquipmentSlot.Weapon);
            var overcoat = PositionOf(EquipmentSlot.Overcoat);
            var helmet = PositionOf(EquipmentSlot.Helmet);
            var overhelm = PositionOf(EquipmentSlot.Hat);
            var shield = PositionOf(EquipmentSlot.Shield);
            var accessory1 =
                PositionOf(EquipmentSlot.Accessory1);
            var accessory2 =
                PositionOf(EquipmentSlot.Accessory2);
            var accessory3 =
                PositionOf(EquipmentSlot.Accessory3);

            Assert.Multiple(() =>
            {
                Assert.That(
                    overcoat,
                    Is.EqualTo(
                        (armor.Row, armor.Column - 1)));
                Assert.That(
                    weapon,
                    Is.EqualTo(
                        (armor.Row + 1, armor.Column)));
                Assert.That(
                    overhelm,
                    Is.EqualTo(
                        (helmet.Row + 1, helmet.Column)));
                Assert.That(
                    accessory1,
                    Is.EqualTo(
                        (shield.Row - 1, shield.Column)));
                Assert.That(
                    accessory2,
                    Is.EqualTo(
                        (shield.Row - 1, shield.Column + 1)));
                Assert.That(
                    accessory3,
                    Is.EqualTo(
                        (shield.Row, shield.Column + 1)));
            });
        }

        private static (int Row, int Column) PositionOf(
            EquipmentSlot slot) =>
            EquipmentSlotPositionConverter.GetPosition(
                (int)slot + 1);
    }
}
