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

            return slot switch
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

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
