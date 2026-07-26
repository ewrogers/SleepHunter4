using SleepHunter.Extensions;
using SleepHunter.Models;

namespace SleepHunter.Tests.Extensions
{
    [TestFixture]
    public sealed class PlayerClassExtensionsTests
    {
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
