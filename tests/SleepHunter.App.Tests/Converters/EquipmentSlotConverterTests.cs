using System.Globalization;
using SleepHunter.Converters;
using SleepHunter.Models;

namespace SleepHunter.Tests.Converters;

public sealed class EquipmentSlotConverterTests
{
    [TestCase(EquipmentSlot.Weapon, "WEAP")]
    [TestCase(EquipmentSlot.Shield, "SHLD")]
    [TestCase(EquipmentSlot.Earring, "EAR")]
    [TestCase(EquipmentSlot.Necklace, "NECK")]
    [TestCase(EquipmentSlot.Belt, "BELT")]
    [TestCase(EquipmentSlot.LeftRing, "LRNG")]
    [TestCase(EquipmentSlot.RightRing, "RRNG")]
    [TestCase(EquipmentSlot.LeftGauntlet, "LARM")]
    [TestCase(EquipmentSlot.RightGauntlet, "RARM")]
    [TestCase(EquipmentSlot.Boots, "FEET")]
    [TestCase(EquipmentSlot.Greaves, "LEGS")]
    [TestCase(EquipmentSlot.Armor, "ARMR")]
    [TestCase(EquipmentSlot.Helmet, "HEAD")]
    [TestCase(EquipmentSlot.Overcoat, "COAT")]
    [TestCase(EquipmentSlot.Accessory1, "ACC1")]
    [TestCase(EquipmentSlot.Accessory2, "ACC2")]
    [TestCase(EquipmentSlot.Accessory3, "ACC3")]
    [TestCase(EquipmentSlot.Hat, "OVER")]
    public void ShouldAbbreviateEveryEquipmentSlot(
        EquipmentSlot slot,
        string expected)
    {
        var converter = new EquipmentSlotConverter();

        Assert.That(
            converter.Convert(
                (int)slot + 1,
                typeof(string),
                "Abbreviation",
                CultureInfo.InvariantCulture),
            Is.EqualTo(expected));
    }
}
