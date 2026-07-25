using System;
using SleepHunter.Settings;

namespace SleepHunter.Services.Clients
{
    public sealed class ClientLaunchOptions
    {
        public ClientLaunchOptions(UserSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            ExecutablePath = settings.ClientPath;
            AllowMultipleInstances =
                settings.AllowMultipleInstances;
            SkipIntroVideo = settings.SkipIntroVideo;
            SuppressLoginNotification =
                settings.SuppressLoginNotification;
            ApplyModifiersKeyFix =
                settings.ApplyModifiersKeyFix;
            AllowAltToShowGroundItems =
                settings.AllowAltToShowGroundItems;
            ImprovedAutoFollow = settings.ImprovedAutoFollow;
            ImprovedAutoFollowMinimumDistance =
                settings.ImprovedAutoFollowMinimumDistance;
            ShowItemQuantitiesInDialogs =
                settings.ShowItemQuantitiesInDialogs;
            MakeExchangeDialogDraggable =
                settings.MakeExchangeDialogDraggable;
            ShowExchangeResultsInMessageBar =
                settings.ShowExchangeResultsInMessageBar;
            NoWalls = settings.NoWalls;
        }

        public string ExecutablePath { get; }

        public bool AllowMultipleInstances { get; }

        public bool SkipIntroVideo { get; }

        public bool SuppressLoginNotification { get; }

        public bool ApplyModifiersKeyFix { get; }

        public bool AllowAltToShowGroundItems { get; }

        public bool ImprovedAutoFollow { get; }

        public int ImprovedAutoFollowMinimumDistance { get; }

        public bool ShowItemQuantitiesInDialogs { get; }

        public bool MakeExchangeDialogDraggable { get; }

        public bool ShowExchangeResultsInMessageBar { get; }

        public bool NoWalls { get; }
    }
}
