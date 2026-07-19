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
            Assert.That(settings.ShowItemQuantitiesInDialogs, Is.True);
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

        private static UserSettings Deserialize(string xml)
        {
            using var reader = new StringReader(xml);
            return (UserSettings)Serializer.Deserialize(reader)!;
        }
    }
}
