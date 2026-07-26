using SleepHunter.Extensions;
using SleepHunter.Models;

namespace SleepHunter.Tests.Extensions
{
    [TestFixture]
    public sealed class PlayerClassExtensionsTests
    {
        [TestCase(0, PlayerClass.Peasant)]
        [TestCase(1, PlayerClass.Warrior)]
        [TestCase(2, PlayerClass.Rogue)]
        [TestCase(3, PlayerClass.Wizard)]
        [TestCase(4, PlayerClass.Priest)]
        [TestCase(5, PlayerClass.Monk)]
        public void ShouldTranslateClientClassValues(byte clientValue, PlayerClass expected)
        {
            var wasParsed = PlayerClassExtensions.TryFromClientValue(clientValue, out var actual);

            Assert.Multiple(() =>
            {
                Assert.That(wasParsed, Is.True);
                Assert.That(actual, Is.EqualTo(expected));
            });
        }

        [Test]
        public void ShouldMatchMetadataClassFlags()
        {
            var allowed = PlayerClass.Wizard | PlayerClass.Priest;

            Assert.Multiple(() =>
            {
                Assert.That(allowed.Includes(PlayerClass.Wizard), Is.True);
                Assert.That(allowed.Includes(PlayerClass.Priest), Is.True);
                Assert.That(allowed.Includes(PlayerClass.Rogue), Is.False);
                Assert.That(PlayerClass.All.Includes(PlayerClass.Monk), Is.True);
                Assert.That(PlayerClass.Peasant.Includes(PlayerClass.Peasant), Is.True);
                Assert.That(PlayerClass.Peasant.Includes(PlayerClass.Warrior), Is.False);
            });
        }
    }
}
