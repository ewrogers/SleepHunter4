
namespace SleepHunter.Macro
{
    public static class LocalStorageKey
    {
        public static class UseWaterAndBeds
        {
            public const string IsEnabled = $"{nameof(UseWaterAndBeds)}.{nameof(IsEnabled)}";
            public const string TileX = $"{nameof(UseWaterAndBeds)}.{nameof(TileX)}";
            public const string TileY = $"{nameof(UseWaterAndBeds)}.{nameof(TileY)}";
            public const string ManaThreshold = $"{nameof(UseWaterAndBeds)}.{nameof(ManaThreshold)}";
        }
    }
}
