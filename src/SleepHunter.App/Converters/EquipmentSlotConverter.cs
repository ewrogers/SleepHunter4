using System;
using System.Globalization;
using System.Windows.Data;
using SleepHunter.Models;

namespace SleepHunter.Converters
{
    public sealed class EquipmentSlotConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var intValue = System.Convert.ToInt32(value);
            var slot = (EquipmentSlot)(intValue - 1);

            return string.Equals(
                parameter?.ToString(),
                "Abbreviation",
                StringComparison.OrdinalIgnoreCase)
                ? GetAbbreviation(slot)
                : GetDisplayName(slot);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();

        internal static string GetAbbreviation(EquipmentSlot slot) =>
            slot switch
            {
                EquipmentSlot.Weapon => "WEAP",
                EquipmentSlot.Shield => "SHLD",
                EquipmentSlot.Earring => "EAR",
                EquipmentSlot.Necklace => "NECK",
                EquipmentSlot.Belt => "BELT",
                EquipmentSlot.LeftRing => "LRNG",
                EquipmentSlot.RightRing => "RRNG",
                EquipmentSlot.LeftGauntlet => "LARM",
                EquipmentSlot.RightGauntlet => "RARM",
                EquipmentSlot.Boots => "FEET",
                EquipmentSlot.Greaves => "LEGS",
                EquipmentSlot.Armor => "ARMR",
                EquipmentSlot.Helmet => "HEAD",
                EquipmentSlot.Overcoat => "COAT",
                EquipmentSlot.Accessory1 => "ACC1",
                EquipmentSlot.Accessory2 => "ACC2",
                EquipmentSlot.Accessory3 => "ACC3",
                EquipmentSlot.Hat => "OVER",
                _ => string.Empty
            };

        private static string GetDisplayName(EquipmentSlot slot) =>
            slot switch
            {
                EquipmentSlot.Weapon => "Main Hand",
                EquipmentSlot.Shield => "Off-Hand",
                EquipmentSlot.Armor => "Armor",
                EquipmentSlot.Helmet => "Helm",
                EquipmentSlot.Earring => "Ear",
                EquipmentSlot.Necklace => "Neck",
                EquipmentSlot.LeftRing => "Left Finger",
                EquipmentSlot.RightRing => "Right Finger",
                EquipmentSlot.LeftGauntlet => "Left Arm",
                EquipmentSlot.RightGauntlet => "Right Arm",
                EquipmentSlot.Belt => "Belt",
                EquipmentSlot.Greaves => "Legs",
                EquipmentSlot.Boots => "Feet",
                EquipmentSlot.Overcoat => "Overcoat",
                EquipmentSlot.Hat => "Head Accessory",
                EquipmentSlot.Accessory1 => "Accessory 1",
                EquipmentSlot.Accessory2 => "Accessory 2",
                EquipmentSlot.Accessory3 => "Accessory 3",
                _ => "Unknown"
            };
    }
}
