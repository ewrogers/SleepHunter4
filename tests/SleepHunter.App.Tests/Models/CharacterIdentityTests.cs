using SleepHunter.Models;

namespace SleepHunter.Tests.Models
{
    [TestFixture]
    public sealed class CharacterIdentityTests
    {
        [TestCase("SiLo", true)]
        [TestCase("Player2", true)]
        [TestCase("A-name", true)]
        [TestCase("", false)]
        [TestCase("1StartsWrong", false)]
        [TestCase("Name WithSpace", false)]
        [TestCase("ThisNameIsTooLong", false)]
        [TestCase("Náme", false)]
        public void ShouldAcceptOnlyPlausibleClientCharacterNames(string name, bool expected)
        {
            Assert.Multiple(() =>
            {
                Assert.That(Player.IsValidCharacterName(name), Is.EqualTo(expected));
                Assert.That(CharacterProfile.IsValidGroupMemberName(name), Is.EqualTo(expected));
            });
        }
    }
}
