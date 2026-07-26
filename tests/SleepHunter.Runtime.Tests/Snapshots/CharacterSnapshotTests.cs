using SleepHunter.Runtime.Characters;
using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Runtime.Tests.Snapshots;

public sealed class CharacterSnapshotTests
{
    [Test]
    public void ShouldPreserveOptionalObservedIdentity()
    {
        var snapshot = new CharacterSnapshot(
            CharacterClass.Wizard,
            level: 99,
            abilityLevel: 50,
            name: "  Aislinn  ",
            characterId: 1234);

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.Name, Is.EqualTo("Aislinn"));
            Assert.That(snapshot.CharacterId, Is.EqualTo(1234));
            Assert.That(snapshot.Class, Is.EqualTo(CharacterClass.Wizard));
            Assert.That(snapshot.Level, Is.EqualTo(99));
            Assert.That(snapshot.AbilityLevel, Is.EqualTo(50));
        });
    }

    [Test]
    public void ShouldRejectWhitespaceObservedName()
    {
        Assert.Throws<ArgumentException>(
            () => _ = new CharacterSnapshot(
                CharacterClass.Wizard,
                level: 1,
                abilityLevel: 0,
                name: " "));
    }
}
