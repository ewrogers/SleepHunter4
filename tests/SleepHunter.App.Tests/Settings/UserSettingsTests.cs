using System.Xml.Serialization;

using SleepHunter.Settings;

namespace SleepHunter.Tests.Settings
{
    [TestFixture]
    public sealed class UserSettingsTests
    {
        private static readonly XmlSerializer Serializer = new(typeof(UserSettings));

        [Test]
        public void ShouldEnableModifiersKeyFixWhenLegacySettingsOmitIt()
        {
            var settings = Deserialize("<UserSettings />");

            Assert.That(settings.ApplyModifiersKeyFix, Is.True);
            Assert.That(settings.AllowAltToShowGroundItems, Is.True);
            Assert.That(settings.ImprovedAutoFollow, Is.True);
            Assert.That(settings.ImprovedAutoFollowMinimumDistance, Is.EqualTo(3));
            Assert.That(settings.ShowItemQuantitiesInDialogs, Is.True);
            Assert.That(settings.MakeExchangeDialogDraggable, Is.True);
            Assert.That(settings.ShowExchangeResultsInMessageBar, Is.False);
        }

        [Test]
        public void ShouldPreserveExplicitlyDisabledModifiersKeyFix()
        {
            var settings = Deserialize(
                "<UserSettings><ApplyModifiersKeyFix>false</ApplyModifiersKeyFix></UserSettings>");

            Assert.That(settings.ApplyModifiersKeyFix, Is.False);
        }

        [Test]
        public void ShouldPreserveExplicitlyDisabledItemQuantitiesInDialogs()
        {
            var settings = Deserialize(
                "<UserSettings><ShowItemQuantitiesInDialogs>false</ShowItemQuantitiesInDialogs></UserSettings>");

            Assert.That(settings.ShowItemQuantitiesInDialogs, Is.False);
        }

        [Test]
        public void ShouldPreserveExplicitlyDisabledAltGroundItems()
        {
            var settings = Deserialize(
                "<UserSettings><AllowAltToShowGroundItems>false</AllowAltToShowGroundItems></UserSettings>");

            Assert.That(settings.AllowAltToShowGroundItems, Is.False);
        }

        [Test]
        public void ShouldPreserveExplicitlyDisabledImprovedAutoFollow()
        {
            var settings = Deserialize(
                "<UserSettings>" +
                "<ImprovedAutoFollow>false</ImprovedAutoFollow>" +
                "<ImprovedAutoFollowMinimumDistance>8</ImprovedAutoFollowMinimumDistance>" +
                "</UserSettings>");

            Assert.Multiple(() =>
            {
                Assert.That(settings.ImprovedAutoFollow, Is.False);
                Assert.That(settings.ImprovedAutoFollowMinimumDistance, Is.EqualTo(8));
            });
        }

        [TestCase(0, 1)]
        [TestCase(11, 10)]
        public void ShouldClampImprovedAutoFollowMinimumDistance(int configuredDistance, int expectedDistance)
        {
            var settings = Deserialize(
                $"<UserSettings><ImprovedAutoFollowMinimumDistance>{configuredDistance}" +
                "</ImprovedAutoFollowMinimumDistance></UserSettings>");

            Assert.That(settings.ImprovedAutoFollowMinimumDistance, Is.EqualTo(expectedDistance));
        }

        [Test]
        public void ShouldPreserveExplicitExchangeUiSettings()
        {
            var settings = Deserialize(
                "<UserSettings>" +
                "<MakeExchangeDialogDraggable>false</MakeExchangeDialogDraggable>" +
                "<ShowExchangeResultsInMessageBar>true</ShowExchangeResultsInMessageBar>" +
                "</UserSettings>");

            Assert.Multiple(() =>
            {
                Assert.That(settings.MakeExchangeDialogDraggable, Is.False);
                Assert.That(settings.ShowExchangeResultsInMessageBar, Is.True);
            });
        }

        private static UserSettings Deserialize(string xml)
        {
            using var reader = new StringReader(xml);
            return (UserSettings)Serializer.Deserialize(reader)!;
        }
    }
}
