using System;
using SleepHunter.Settings;

namespace SleepHunter.Services.Clients
{
    internal sealed record ClientPatchPlan(
        bool ApplyMultipleInstances,
        bool SkipIntroVideo,
        bool SuppressLoginNotification,
        bool ApplyModifiersKeyFix,
        bool AllowAltToShowGroundItems,
        bool ApplyImprovedAutoFollow,
        bool ShowItemQuantitiesInDialogs,
        bool MakeExchangeDialogDraggable,
        bool ShowExchangeResultsInMessageBar,
        bool RemoveWalls)
    {
        public bool HasRuntimePatches =>
            ApplyModifiersKeyFix ||
            AllowAltToShowGroundItems ||
            ApplyImprovedAutoFollow ||
            ShowItemQuantitiesInDialogs ||
            MakeExchangeDialogDraggable ||
            ShowExchangeResultsInMessageBar;

        public bool HasClientPatches =>
            ApplyMultipleInstances ||
            SkipIntroVideo ||
            SuppressLoginNotification ||
            RemoveWalls ||
            HasRuntimePatches;

        public static ClientPatchPlan Create(
            ClientLaunchOptions options,
            ClientLayout layout)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(layout);

            var allowAltToShowGroundItems =
                options.AllowAltToShowGroundItems &&
                layout.SupportsAltToShowGroundItems;
            return new ClientPatchPlan(
                options.AllowMultipleInstances &&
                    layout.MultipleInstanceAddress > 0,
                options.SkipIntroVideo &&
                    layout.IntroVideoAddress > 0,
                options.SuppressLoginNotification &&
                    layout.SupportsLoginNotificationSuppression,
                (options.ApplyModifiersKeyFix ||
                    allowAltToShowGroundItems) &&
                    layout.SupportsModifiersKeyFix,
                allowAltToShowGroundItems,
                options.ImprovedAutoFollow &&
                    layout.SupportsImprovedAutoFollow,
                options.ShowItemQuantitiesInDialogs &&
                    layout.SupportsItemQuantitiesInDialogs,
                options.MakeExchangeDialogDraggable &&
                    layout.SupportsDraggableExchangeDialog,
                options.ShowExchangeResultsInMessageBar &&
                    layout.SupportsExchangeResultsInMessageBar,
                options.NoWalls &&
                    layout.NoWallAddress > 0);
        }
    }
}
