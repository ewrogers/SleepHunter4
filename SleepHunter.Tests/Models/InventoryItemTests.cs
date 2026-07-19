using System.Buffers.Binary;
using System.Text;

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
            var item = new InventoryItem(60, "Gold", iconIndex: 136, quantity: 1234567)
            {
                IsGold = true
            };

            Assert.Multiple(() =>
            {
                Assert.That(item.Name, Is.EqualTo("Gold"));
                Assert.That(item.FormattedQuantity, Is.EqualTo("1,234,567"));
                Assert.That(item.QuantityBadgeText, Is.EqualTo("1,234,567"));
                Assert.That(item.IsGold, Is.True);
                Assert.That(item.ShowsQuantity, Is.True);
            });
        }

        [Test]
        public void ShouldParseCompactInventoryRecordsWithoutStrippingNameDigits()
        {
            var snapshot = new byte[Inventory.InventoryRecordSize * 2];
            snapshot[0] = 1;
            snapshot[1] = 0xCC;
            BinaryPrimitives.WriteUInt16LittleEndian(snapshot.AsSpan(2), 0x8123);
            snapshot[4] = 7;
            Encoding.ASCII.GetBytes("Ring2").CopyTo(snapshot.AsSpan(5));
            snapshot[Inventory.InventoryRecordSize] = 0;

            var records = Inventory.ParseInventorySnapshot(snapshot, 2);

            Assert.Multiple(() =>
            {
                Assert.That(records[0].IsPresent, Is.True);
                Assert.That(records[0].RawSprite, Is.EqualTo(0x8123));
                Assert.That(records[0].DyeColor, Is.EqualTo(7));
                Assert.That(records[0].Name, Is.EqualTo("Ring2"));
                Assert.That(records[1].IsPresent, Is.False);
            });
        }

        [TestCase(0x0000, 0)]
        [TestCase(0x8000, 0)]
        [TestCase(0x8001, 1)]
        [TestCase(0x8123, 0x123)]
        public void ShouldDecodeInventorySprites(int rawSprite, int expectedItemId)
        {
            Assert.That(IconManager.DecodeInventorySprite((ushort)rawSprite), Is.EqualTo(expectedItemId));
        }

        [Test]
        public void ShouldParseLiveInventoryPaneQuantity()
        {
            var snapshot = new byte[Inventory.InventoryPaneSnapshotSize];
            BinaryPrimitives.WriteUInt16LittleEndian(snapshot, 0x8123);
            snapshot[0x82] = 6;
            BinaryPrimitives.WriteUInt32LittleEndian(snapshot.AsSpan(0xA8), 12345);
            BinaryPrimitives.WriteUInt32LittleEndian(snapshot.AsSpan(0xAC), 15000);
            BinaryPrimitives.WriteUInt32LittleEndian(snapshot.AsSpan(0xB0), 1234567);
            snapshot[0xB4] = 1;

            var record = Inventory.ParseInventoryPaneSnapshot(snapshot);

            Assert.Multiple(() =>
            {
                Assert.That(record.IsValid, Is.True);
                Assert.That(record.RawSprite, Is.EqualTo(0x8123));
                Assert.That(record.DyeColor, Is.EqualTo(6));
                Assert.That(record.MaximumDurability, Is.EqualTo(15000));
                Assert.That(record.Durability, Is.EqualTo(12345));
                Assert.That(record.Quantity, Is.EqualTo(1234567));
                Assert.That(record.CanStack, Is.True);
            });
        }

        [Test]
        public void ShouldShowQuantityForItemStacks()
        {
            var item = new InventoryItem(1, "Viper's Gland", quantity: 11);

            Assert.Multiple(() =>
            {
                Assert.That(item.FormattedQuantity, Is.EqualTo("11"));
                Assert.That(item.QuantityBadgeText, Is.EqualTo("x11"));
                Assert.That(item.ShowsQuantity, Is.True);
            });
        }

        [Test]
        public void ShouldFormatDecodedSpriteAndDurabilityForTooltip()
        {
            var item = new InventoryItem(1, "Bardocle", iconIndex: 0x809A)
            {
                Durability = 12345,
                MaximumDurability = 15000
            };

            Assert.Multiple(() =>
            {
                Assert.That(item.SpriteNumber, Is.EqualTo(154));
                Assert.That(item.HasDurability, Is.True);
                Assert.That(item.FormattedDurability, Is.EqualTo("12345 / 15000"));
            });
        }
    }
}
