using SleepHunter.Models;

namespace SleepHunter.Tests.Models
{
    [TestFixture]
    public sealed class AbilityPresentationTests
    {
        [TestCase(1, 1)]
        [TestCase(36, 36)]
        [TestCase(37, 1)]
        [TestCase(72, 36)]
        [TestCase(73, 1)]
        [TestCase(90, 18)]
        public void ShouldDisplayOneBasedSlotsWithinEachAbilityPane(
            int slot,
            int expectedRelativeSlot)
        {
            var ability = new Skill { Slot = slot };

            Assert.That(
                ability.RelativeSlot,
                Is.EqualTo(expectedRelativeSlot));
        }

        [TestCase(0u, 1.0)]
        [TestCase(1u, 29.0 / 30.0)]
        [TestCase(15u, 0.5)]
        [TestCase(29u, 1.0 / 30.0)]
        [TestCase(30u, 0.0)]
        [TestCase(31u, 0.0)]
        public void ShouldConvertCooldownStepsIntoARemainingOverlay(
            uint progress,
            double expectedRemainingFraction)
        {
            var skill = new Skill
            {
                CooldownProgress = progress
            };

            Assert.That(
                skill.CooldownRemainingFraction,
                Is.EqualTo(expectedRemainingFraction)
                    .Within(0.000001));
        }
    }
}
