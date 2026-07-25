using SleepHunter.Services.Clients;
using SleepHunter.Settings;

namespace SleepHunter.Tests.Services.Clients
{
    public sealed class ClientPatchPlanTests
    {
        [Test]
        public void ShouldSelectRequestedSupportedPatches()
        {
            var settings = RequestedSettings();
            var layout = SupportedLayout();

            var plan = ClientPatchPlan.Create(
                new ClientLaunchOptions(settings),
                layout);

            Assert.Multiple(() =>
            {
                Assert.That(
                    plan.ApplyMultipleInstances,
                    Is.True);
                Assert.That(plan.SkipIntroVideo, Is.True);
                Assert.That(
                    plan.SuppressLoginNotification,
                    Is.True);
                Assert.That(
                    plan.ApplyModifiersKeyFix,
                    Is.True);
                Assert.That(
                    plan.AllowAltToShowGroundItems,
                    Is.True);
                Assert.That(
                    plan.ApplyImprovedAutoFollow,
                    Is.True);
                Assert.That(
                    plan.ShowItemQuantitiesInDialogs,
                    Is.True);
                Assert.That(
                    plan.MakeExchangeDialogDraggable,
                    Is.True);
                Assert.That(
                    plan.ShowExchangeResultsInMessageBar,
                    Is.True);
                Assert.That(plan.RemoveWalls, Is.True);
                Assert.That(
                    plan.HasRuntimePatches,
                    Is.True);
                Assert.That(
                    plan.HasClientPatches,
                    Is.True);
            });
        }

        [Test]
        public void ShouldSkipUnsupportedOrUnmappedPatches()
        {
            var settings = RequestedSettings();
            var layout = new ClientLayout();

            var plan = ClientPatchPlan.Create(
                new ClientLaunchOptions(settings),
                layout);

            Assert.Multiple(() =>
            {
                Assert.That(
                    plan.ApplyMultipleInstances,
                    Is.False);
                Assert.That(
                    plan.SkipIntroVideo,
                    Is.False);
                Assert.That(
                    plan.SuppressLoginNotification,
                    Is.False);
                Assert.That(
                    plan.ApplyModifiersKeyFix,
                    Is.False);
                Assert.That(
                    plan.AllowAltToShowGroundItems,
                    Is.False);
                Assert.That(
                    plan.ApplyImprovedAutoFollow,
                    Is.False);
                Assert.That(
                    plan.ShowItemQuantitiesInDialogs,
                    Is.False);
                Assert.That(
                    plan.MakeExchangeDialogDraggable,
                    Is.False);
                Assert.That(
                    plan.ShowExchangeResultsInMessageBar,
                    Is.False);
                Assert.That(plan.RemoveWalls, Is.False);
                Assert.That(
                    plan.HasRuntimePatches,
                    Is.False);
                Assert.That(
                    plan.HasClientPatches,
                    Is.False);
            });
        }

        [Test]
        public void ShouldApplyModifierFixRequiredByGroundItemPatch()
        {
            var settings = RequestedSettings();
            settings.ApplyModifiersKeyFix = false;
            var layout = SupportedLayout();

            var plan = ClientPatchPlan.Create(
                new ClientLaunchOptions(settings),
                layout);

            Assert.Multiple(() =>
            {
                Assert.That(
                    plan.AllowAltToShowGroundItems,
                    Is.True);
                Assert.That(
                    plan.ApplyModifiersKeyFix,
                    Is.True);
            });
        }

        [Test]
        public void ShouldSnapshotMutableUserSettings()
        {
            var settings = RequestedSettings();
            var options = new ClientLaunchOptions(settings);

            settings.ClientPath = "changed.exe";
            settings.ImprovedAutoFollowMinimumDistance = 9;

            Assert.Multiple(() =>
            {
                Assert.That(
                    options.ExecutablePath,
                    Is.EqualTo("client.exe"));
                Assert.That(
                    options.ImprovedAutoFollowMinimumDistance,
                    Is.EqualTo(3));
            });
        }

        private static UserSettings RequestedSettings() =>
            new()
            {
                ClientPath = "client.exe",
                AllowMultipleInstances = true,
                SkipIntroVideo = true,
                SuppressLoginNotification = true,
                ApplyModifiersKeyFix = true,
                AllowAltToShowGroundItems = true,
                ImprovedAutoFollow = true,
                ImprovedAutoFollowMinimumDistance = 3,
                ShowItemQuantitiesInDialogs = true,
                MakeExchangeDialogDraggable = true,
                ShowExchangeResultsInMessageBar = true,
                NoWalls = true
            };

        private static ClientLayout SupportedLayout() =>
            new()
            {
                MultipleInstanceAddress = 0x1000,
                IntroVideoAddress = 0x2000,
                NoWallAddress = 0x3000,
                SupportsLoginNotificationSuppression =
                    true,
                SupportsModifiersKeyFix = true,
                SupportsAltToShowGroundItems = true,
                SupportsImprovedAutoFollow = true,
                SupportsItemQuantitiesInDialogs = true,
                SupportsDraggableExchangeDialog = true,
                SupportsExchangeResultsInMessageBar =
                    true
            };
    }
}
