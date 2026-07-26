using SleepHunter.Media;
using SleepHunter.Models;

namespace SleepHunter.Tests.Models
{
    [TestFixture]
    public sealed class InventoryItemTests
    {
        [Test]
        public void ShouldFormatGoldQuantitySeparatelyFromName()
        {
            var item = new InventoryItem(
                60,
                "Gold",
                iconIndex: 136,
                quantity: 1234567)
            {
                IsGold = true
            };

            Assert.Multiple(() =>
            {
                Assert.That(item.Name, Is.EqualTo("Gold"));
                Assert.That(
                    item.FormattedQuantity,
                    Is.EqualTo("1,234,567"));
                Assert.That(
                    item.QuantityBadgeText,
                    Is.EqualTo("1.2m"));
                Assert.That(item.IsGold, Is.True);
                Assert.That(item.ShowsQuantity, Is.True);
            });
        }

        [TestCase(646, "646")]
        [TestCase(1_000, "1k")]
        [TestCase(100_600, "100.6k")]
        [TestCase(999_949, "999.9k")]
        [TestCase(999_950, "1m")]
        [TestCase(2_400_000, "2.4m")]
        public void ShouldCompactGoldQuantityInSlotBadge(
            int quantity,
            string expectedBadgeText)
        {
            var item = new InventoryItem(
                60,
                "Gold",
                quantity: quantity)
            {
                IsGold = true
            };

            Assert.Multiple(() =>
            {
                Assert.That(
                    item.QuantityBadgeText,
                    Is.EqualTo(expectedBadgeText));
                Assert.That(
                    item.FormattedQuantity,
                    Is.EqualTo(quantity.ToString("N0", System.Globalization.CultureInfo.InvariantCulture)));
            });
        }

        [TestCase(0x0000, 0)]
        [TestCase(0x8000, 0)]
        [TestCase(0x8001, 1)]
        [TestCase(0x8123, 0x123)]
        public void ShouldDecodeInventorySprites(
            int rawSprite,
            int expectedItemId)
        {
            Assert.That(
                IconManager.DecodeInventorySprite(
                    (ushort)rawSprite),
                Is.EqualTo(expectedItemId));
        }

        [Test]
        public void ShouldShowQuantityForItemStacks()
        {
            var item = new InventoryItem(
                1,
                "Viper's Gland",
                quantity: 11);

            Assert.Multiple(() =>
            {
                Assert.That(
                    item.FormattedQuantity,
                    Is.EqualTo("11"));
                Assert.That(
                    item.QuantityBadgeText,
                    Is.EqualTo("x11"));
                Assert.That(item.ShowsQuantity, Is.True);
            });
        }

        [Test]
        public void ShouldFormatDecodedSpriteAndDurabilityForTooltip()
        {
            var item = new InventoryItem(
                1,
                "Bardocle",
                iconIndex: 0x809A)
            {
                Durability = 12345,
                MaximumDurability = 15000
            };

            Assert.Multiple(() =>
            {
                Assert.That(item.SpriteNumber, Is.EqualTo(154));
                Assert.That(item.HasDurability, Is.True);
                Assert.That(
                    item.FormattedDurability,
                    Is.EqualTo("12345 / 15000"));
            });
        }
    }
}
