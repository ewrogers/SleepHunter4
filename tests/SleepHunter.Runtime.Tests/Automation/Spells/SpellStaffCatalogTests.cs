using SleepHunter.Runtime.Automation.Spells;
using SleepHunter.Runtime.Automation.Staves;
using SleepHunter.Runtime.Characters;

namespace SleepHunter.Runtime.Tests.Automation.Spells;

public sealed class SpellStaffCatalogTests
{
    [Test]
    public void ShouldReturnImmutableCandidatesByQueueEntry()
    {
        var first = Candidate("first");
        var second = Candidate("second");
        var set = new SpellStaffCandidateSet(
            new SpellQueueEntryId(2),
            [first, second]);
        var catalog = new SpellStaffCatalog([set]);

        Assert.Multiple(() =>
        {
            var candidates =
                catalog.GetCandidates(new SpellQueueEntryId(2));
            Assert.That(candidates, Has.Length.EqualTo(2));
            Assert.That(candidates[0], Is.EqualTo(first));
            Assert.That(candidates[1], Is.EqualTo(second));
            Assert.That(
                catalog.GetCandidates(new SpellQueueEntryId(1)),
                Is.Empty);
        });
    }

    [Test]
    public void ShouldValidateCatalogValues()
    {
        var entryId = new SpellQueueEntryId(1);
        var candidate = Candidate("staff");

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = new SpellStaffCandidateSet(default, [candidate]));
            Assert.Throws<ArgumentException>(
                () => _ = new SpellStaffCandidateSet(
                    entryId,
                    [candidate, Candidate(" STAFF ")]));
            Assert.Throws<ArgumentException>(
                () => _ = new SpellStaffCatalog(
                [
                    new SpellStaffCandidateSet(entryId, [candidate]),
                    new SpellStaffCandidateSet(entryId, [])
                ]));
        });
    }

    [Test]
    public void ShouldCompareIndependentCatalogsByValue()
    {
        var first = new SpellStaffCatalog(
        [
            new SpellStaffCandidateSet(
                new SpellQueueEntryId(2),
                [Candidate("second")]),
            new SpellStaffCandidateSet(
                new SpellQueueEntryId(1),
                [Candidate("first")])
        ]);
        var second = new SpellStaffCatalog(
        [
            new SpellStaffCandidateSet(
                new SpellQueueEntryId(1),
                [Candidate("first")]),
            new SpellStaffCandidateSet(
                new SpellQueueEntryId(2),
                [Candidate("second")])
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(second));
            Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
        });
    }

    private static StaffCandidate Candidate(string name) =>
        new(
            name,
            CharacterClass.Wizard,
            requiredLevel: 0,
            requiredAbilityLevel: 0,
            castLines: 1);
}
