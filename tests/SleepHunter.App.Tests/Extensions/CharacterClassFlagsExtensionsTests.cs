using SleepHunter.Extensions;
using SleepHunter.Models;

namespace SleepHunter.Tests.Extensions
{
    [TestFixture]
    public sealed class CharacterClassFlagsExtensionsTests
    {
        [Test]
        public void ShouldMatchMetadataClassFlags()
        {
            var allowed = CharacterClassFlags.Wizard | CharacterClassFlags.Priest;

            Assert.Multiple(() =>
            {
                Assert.That(allowed.Includes(CharacterClassFlags.Wizard), Is.True);
                Assert.That(allowed.Includes(CharacterClassFlags.Priest), Is.True);
                Assert.That(allowed.Includes(CharacterClassFlags.Rogue), Is.False);
                Assert.That(CharacterClassFlags.All.Includes(CharacterClassFlags.Monk), Is.True);
                Assert.That(CharacterClassFlags.Peasant.Includes(CharacterClassFlags.Peasant), Is.True);
                Assert.That(CharacterClassFlags.Peasant.Includes(CharacterClassFlags.Warrior), Is.False);
            });
        }
    }
}
