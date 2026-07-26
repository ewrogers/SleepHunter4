using System;
using System.Globalization;
using System.Windows.Data;

namespace SleepHunter.Converters
{
    public sealed class EquipmentSlotPositionConverter : IValueConverter
    {
        private static readonly (int Row, int Column)[] SlotPositions =
        {
            (2, 1), // Weapon
            (1, 1), // Armor
            (2, 4), // Shield
            (0, 2), // Helmet
            (0, 1), // Earring
            (0, 3), // Necklace
            (3, 1), // Left Ring
            (3, 4), // Right Ring
            (3, 0), // Left Gauntlet
            (3, 5), // Right Gauntlet
            (4, 3), // Belt
            (4, 1), // Greaves
            (4, 2), // Boots
            (1, 4), // Accessory 1
            (1, 0), // Overcoat
            (1, 2), // Hat / Overhelm
            (1, 5), // Accessory 2
            (2, 5)  // Accessory 3
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var position = GetPosition(System.Convert.ToInt32(value));
            return string.Equals(parameter?.ToString(), "Column", StringComparison.OrdinalIgnoreCase)
                ? position.Column
                : position.Row;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();

        internal static (int Row, int Column) GetPosition(int oneBasedSlot)
        {
            var slotIndex = oneBasedSlot - 1;
            if (slotIndex < 0 || slotIndex >= SlotPositions.Length)
                return (0, 0);

            return SlotPositions[slotIndex];
        }
    }
}
