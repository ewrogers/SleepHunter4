using SleepHunter.Runtime.Automation.Skills;

namespace SleepHunter.Runtime.Tests.Automation.Skills;

public sealed class SkillQueueStateTests
{
    [Test]
    public void ShouldValidateEntriesAndIgnoreDuplicateAdds()
    {
        var entry = Entry(1, "first");
        var queue = SkillQueueState.Empty.Add(entry);
        var duplicateName = Entry(2, " FIRST ");

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = new SkillQueueEntryId(0));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = new SkillQueueEntry(default, "invalid"));
            Assert.Throws<ArgumentException>(
                () => _ = new SkillQueueEntry(new SkillQueueEntryId(2), " "));
            Assert.That(queue.Add(entry), Is.SameAs(queue));
            Assert.That(queue.Add(duplicateName), Is.SameAs(queue));
            Assert.That(queue.Entries, Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void ShouldRotateAcrossReadyEntriesAndSkipBlockedEntries()
    {
        var first = Entry(1, "first");
        var blocked = Entry(2, "blocked");
        var third = Entry(3, "third");
        var queue = CreateQueue(first, blocked, third);
        var availability = Availability(
            (first.Id, SkillQueueAvailability.Ready),
            (blocked.Id, SkillQueueAvailability.TemporarilyUnavailable),
            (third.Id, SkillQueueAvailability.Ready));

        var firstSelection = queue.EvaluateNext(availability);
        var secondSelection = firstSelection.State.EvaluateNext(availability);
        var thirdSelection = secondSelection.State.EvaluateNext(availability);

        Assert.Multiple(() =>
        {
            Assert.That(firstSelection.SelectedEntry, Is.EqualTo(first));
            Assert.That(firstSelection.State.Cursor, Is.EqualTo(1));
            Assert.That(secondSelection.SelectedEntry, Is.EqualTo(third));
            Assert.That(secondSelection.State.Cursor, Is.Zero);
            Assert.That(thirdSelection.SelectedEntry, Is.EqualTo(first));
        });
    }

    [Test]
    public void ShouldPreserveLogicalCursorAcrossEdits()
    {
        var first = Entry(1, "first");
        var second = Entry(2, "second");
        var third = Entry(3, "third");
        var queue = CreateQueue(first, second, third);
        var availability = Availability(
            (first.Id, SkillQueueAvailability.Ready));
        queue = queue.EvaluateNext(availability).State;

        var inserted = queue.Add(Entry(4, "inserted"), index: 0);
        var moved = inserted.Move(first.Id, targetIndex: 3);
        var updated = moved.Update(new SkillQueueEntry(second.Id, "renamed"));
        var removed = updated.Remove(second.Id);

        Assert.Multiple(() =>
        {
            Assert.That(
                inserted.Entries[inserted.Cursor].Id,
                Is.EqualTo(second.Id));
            Assert.That(
                moved.Entries[moved.Cursor].Id,
                Is.EqualTo(second.Id));
            Assert.That(updated.Entries[updated.Cursor].Name, Is.EqualTo("renamed"));
            Assert.That(
                removed.Entries[removed.Cursor].Id,
                Is.EqualTo(third.Id));
        });
    }

    [Test]
    public void ShouldKeepCursorWhenNoSkillIsReady()
    {
        var first = Entry(1, "first");
        var second = Entry(2, "second");
        var queue = CreateQueue(first, second);
        var ready = Availability((first.Id, SkillQueueAvailability.Ready));
        queue = queue.EvaluateNext(ready).State;

        var result = queue.EvaluateNext(
            Availability(
                (first.Id, SkillQueueAvailability.TemporarilyUnavailable),
                (second.Id, SkillQueueAvailability.Missing)));

        Assert.Multiple(() =>
        {
            Assert.That(result.HasSelection, Is.False);
            Assert.That(result.State, Is.SameAs(queue));
            Assert.That(result.State.Cursor, Is.EqualTo(1));
        });
    }

    private static SkillQueueState CreateQueue(
        params SkillQueueEntry[] entries)
    {
        var queue = SkillQueueState.Empty;
        foreach (var entry in entries)
        {
            queue = queue.Add(entry);
        }

        return queue;
    }

    private static SkillQueueEntry Entry(long id, string name) =>
        new(new SkillQueueEntryId(id), name);

    private static Dictionary<
        SkillQueueEntryId,
        SkillQueueAvailability> Availability(
        params (SkillQueueEntryId Id, SkillQueueAvailability Availability)[]
            entries) =>
        entries.ToDictionary(entry => entry.Id, entry => entry.Availability);
}
