using SleepHunter.Runtime.Automation.Flowering;
using SleepHunter.Runtime.Automation.Spells;

namespace SleepHunter.Runtime.Tests.Automation.Flowering;

public sealed class FlowerQueueStateTests
{
    [Test]
    public void ShouldEditQueueByStableIdentifier()
    {
        var first = Entry(1, SpellTarget.Self);
        var second = Entry(2, SpellTarget.RelativeTile(1, 0));
        var replacement = new FlowerQueueEntry(
            first.Id,
            SpellTarget.RelativeTile(0, 1),
            interval: TimeSpan.FromSeconds(2));

        var queue = FlowerQueueState.Empty
            .Add(first)
            .Add(second)
            .Move(second.Id, 0)
            .Update(replacement);
        var duplicate = queue.Add(
            Entry(second.Id.Value, SpellTarget.Self));
        var removed = queue.Remove(second.Id);

        Assert.Multiple(() =>
        {
            Assert.That(
                queue.Entries.Select(entry => entry.Id),
                Is.EqualTo(new[] { second.Id, first.Id }));
            Assert.That(queue.Entries[1], Is.EqualTo(replacement));
            Assert.That(duplicate, Is.SameAs(queue));
            Assert.That(
                removed.Entries,
                Is.EqualTo(new[] { replacement }));
            Assert.That(removed.Cursor, Is.Zero);
            Assert.That(removed.Clear().Entries, Is.Empty);
        });
    }

    [Test]
    public void ShouldRequireValidTargetAndReadinessCondition()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                () => new FlowerQueueEntry(
                    new FlowerQueueEntryId(1),
                    SpellTarget.None,
                    interval: TimeSpan.Zero),
                Throws.ArgumentException);
            Assert.That(
                () => new FlowerQueueEntry(
                    new FlowerQueueEntryId(1),
                    SpellTarget.Self),
                Throws.ArgumentException);
            Assert.That(
                () => new FlowerQueueEntry(
                    new FlowerQueueEntryId(1),
                    SpellTarget.Self,
                    manaThreshold: 100),
                Throws.ArgumentException);
            Assert.That(
                () => new FlowerQueueEntry(
                    new FlowerQueueEntryId(1),
                    SpellTarget.Self,
                    interval: TimeSpan.FromTicks(-1)),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }

    private static FlowerQueueEntry Entry(
        long id,
        SpellTarget target) =>
        Entry(new FlowerQueueEntryId(id), target);

    private static FlowerQueueEntry Entry(
        FlowerQueueEntryId id,
        SpellTarget target) =>
        new(
            id,
            target,
            interval: TimeSpan.Zero);
}
