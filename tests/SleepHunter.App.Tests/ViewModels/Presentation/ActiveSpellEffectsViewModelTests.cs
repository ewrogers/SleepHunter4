using SleepHunter.Runtime.Snapshots;
using SleepHunter.ViewModels.Presentation;

namespace SleepHunter.Tests.ViewModels.Presentation
{
    public sealed class ActiveSpellEffectsViewModelTests
    {
        [TestCase(SpellEffectDurationStage.Blue, 1)]
        [TestCase(SpellEffectDurationStage.Green, 2)]
        [TestCase(SpellEffectDurationStage.Yellow, 3)]
        [TestCase(SpellEffectDurationStage.Orange, 4)]
        [TestCase(SpellEffectDurationStage.Red, 5)]
        [TestCase(SpellEffectDurationStage.White, 6)]
        public void ShouldProjectDurationEnumsAsHardProgressSteps(
            SpellEffectDurationStage stage,
            int expectedStep)
        {
            var effects = new ActiveSpellEffectsViewModel();

            effects.Apply(
                new ActiveSpellEffectsSnapshot(
                [
                    new ActiveSpellEffectSnapshot(
                        slot: 4,
                        icon: 73,
                        stage)
                ]));

            var effect = effects.Effects[3];
            Assert.Multiple(() =>
            {
                Assert.That(effects.HasEffects, Is.True);
                Assert.That(effect.IsEmpty, Is.False);
                Assert.That(effect.Slot, Is.EqualTo(4));
                Assert.That(effect.IconIndex, Is.EqualTo(73));
                Assert.That(effect.DurationStage, Is.EqualTo(stage));
                Assert.That(effect.DurationStep, Is.EqualTo(expectedStep));
                Assert.That(
                    effect.ToolTipText,
                    Is.EqualTo($"Effect 73 ({stage})"));
            });
        }

        [Test]
        public void ShouldClearEffectsMissingFromTheNextSnapshot()
        {
            var effects = new ActiveSpellEffectsViewModel();
            effects.Apply(
                new ActiveSpellEffectsSnapshot(
                [
                    new ActiveSpellEffectSnapshot(
                        slot: 10,
                        icon: 91,
                        SpellEffectDurationStage.White)
                ]));

            effects.Apply(ActiveSpellEffectsSnapshot.Empty);

            Assert.Multiple(() =>
            {
                Assert.That(effects.HasEffects, Is.False);
                Assert.That(
                    effects.Effects,
                    Has.Count.EqualTo(
                        ActiveSpellEffectSnapshot.MaximumSlot));
                Assert.That(
                    effects.Effects,
                    Has.All.Matches<ActiveSpellEffectViewModel>(
                        effect =>
                            effect.IsEmpty &&
                            effect.DurationStep == 0));
            });
        }
    }
}
