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
        }

        [Test]
        public void ShouldPreserveExplicitlyDisabledModifiersKeyFix()
        {
            var settings = Deserialize(
                "<UserSettings><ApplyModifiersKeyFix>false</ApplyModifiersKeyFix></UserSettings>");

            Assert.That(settings.ApplyModifiersKeyFix, Is.False);
        }

        private static UserSettings Deserialize(string xml)
        {
            using var reader = new StringReader(xml);
            return (UserSettings)Serializer.Deserialize(reader)!;
        }
    }
}
