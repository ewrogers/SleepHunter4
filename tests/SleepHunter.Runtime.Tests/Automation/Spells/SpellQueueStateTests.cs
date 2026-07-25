using SleepHunter.Runtime.Automation.Spells;

namespace SleepHunter.Runtime.Tests.Automation.Spells;

public sealed class SpellQueueStateTests
{
    [Test]
    public void ShouldValidateSpellQueueEntryValues()
    {
        var entry = new SpellQueueEntry(
            new SpellQueueEntryId(1),
            "  test  ",
            targetLevel: 10);

        Assert.Multiple(() =>
        {
            Assert.That(entry.Name, Is.EqualTo("test"));
            Assert.That(entry.TargetLevel, Is.EqualTo(10));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = new SpellQueueEntry(default, "test"));
            Assert.Throws<ArgumentException>(
                () => _ = new SpellQueueEntry(
                    new SpellQueueEntryId(2),
                    " "));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = new SpellQueueEntry(
                    new SpellQueueEntryId(3),
                    "test",
                    targetLevel: 0));
        });
    }

    [Test]
    public void ShouldPreserveLogicalCursorAcrossQueueEdits()
    {
        var first = CreateEntry(1, "first");
        var second = CreateEntry(2, "second");
        var third = CreateEntry(3, "third");
        var inserted = CreateEntry(4, "inserted");
        var state = SpellQueueState.Empty
            .Add(first)
            .Add(second)
            .Add(third)
            .SetRotation(SpellQueueRotation.RoundRobin);
        var availability = Ready(first, second, third, inserted);

        state = state.EvaluateNext(availability).State;
        state = state.Add(inserted, index: 0);
        state = state.Move(third.Id, targetIndex: 1);
        state = state.Remove(first.Id);

        Assert.Multiple(() =>
        {
            Assert.That(
                state.Entries.Select(entry => entry.Id.Value),
                Is.EqualTo(new long[] { 4, 3, 2 }));
            Assert.That(state.Cursor, Is.EqualTo(2));
            Assert.That(state.Entries[state.Cursor], Is.EqualTo(second));
        });

        state = state.Remove(second.Id);

        Assert.Multiple(() =>
        {
            Assert.That(
                state.Entries.Select(entry => entry.Id.Value),
                Is.EqualTo(new long[] { 4, 3 }));
            Assert.That(state.Cursor, Is.Zero);
            Assert.That(state.Entries[state.Cursor], Is.EqualTo(inserted));
        });
    }

    [Test]
    public void ShouldSelectFirstReadyEntryInPriorityMode()
    {
        var first = CreateEntry(1, "first");
        var second = CreateEntry(2, "second");
        var third = CreateEntry(3, "third");
        var state = SpellQueueState.Empty
            .Add(first)
            .Add(second)
            .Add(third);
        var availability = new Dictionary<
            SpellQueueEntryId,
            SpellQueueAvailability>
        {
            [first.Id] = SpellQueueAvailability.TemporarilyUnavailable,
            [second.Id] = SpellQueueAvailability.Ready,
            [third.Id] = SpellQueueAvailability.Ready
        };

        var evaluation = state.EvaluateNext(availability);

        Assert.Multiple(() =>
        {
            Assert.That(evaluation.SelectedEntry, Is.EqualTo(second));
            Assert.That(evaluation.State.Cursor, Is.Zero);
        });
    }

    [Test]
    public void ShouldKeepSequentialCursorOnTemporaryBlock()
    {
        var first = CreateEntry(1, "first");
        var second = CreateEntry(2, "second");
        var third = CreateEntry(3, "third");
        var state = SpellQueueState.Empty
            .Add(first)
            .Add(second)
            .Add(third)
            .SetRotation(SpellQueueRotation.Sequential);
        var availability = new Dictionary<
            SpellQueueEntryId,
            SpellQueueAvailability>
        {
            [first.Id] = SpellQueueAvailability.Complete,
            [second.Id] = SpellQueueAvailability.TemporarilyUnavailable,
            [third.Id] = SpellQueueAvailability.Ready
        };

        var blocked = state.EvaluateNext(availability);
        availability[second.Id] = SpellQueueAvailability.Ready;
        var ready = blocked.State.EvaluateNext(availability);
        availability[second.Id] = SpellQueueAvailability.Complete;
        var advanced = ready.State.EvaluateNext(availability);

        Assert.Multiple(() =>
        {
            Assert.That(blocked.HasSelection, Is.False);
            Assert.That(blocked.State.Cursor, Is.EqualTo(1));
            Assert.That(ready.SelectedEntry, Is.EqualTo(second));
            Assert.That(ready.State.Cursor, Is.EqualTo(1));
            Assert.That(advanced.SelectedEntry, Is.EqualTo(third));
            Assert.That(advanced.State.Cursor, Is.EqualTo(2));
        });
    }

    [Test]
    public void ShouldRotateRoundRobinDeterministically()
    {
        var first = CreateEntry(1, "first");
        var second = CreateEntry(2, "second");
        var third = CreateEntry(3, "third");
        var state = SpellQueueState.Empty
            .Add(first)
            .Add(second)
            .Add(third)
            .SetRotation(SpellQueueRotation.RoundRobin);
        var availability = Ready(first, second, third);
        var selectedIds = new List<long>();

        for (var iteration = 0; iteration < 4; iteration++)
        {
            var evaluation = state.EvaluateNext(availability);
            selectedIds.Add(evaluation.SelectedEntry!.Id.Value);
            state = evaluation.State;
        }

        Assert.Multiple(() =>
        {
            Assert.That(selectedIds, Is.EqualTo(new long[] { 1, 2, 3, 1 }));
            Assert.That(state.Cursor, Is.EqualTo(1));
        });
    }

    [Test]
    public void ShouldCompareIndependentQueueStatesByValue()
    {
        var first = SpellQueueState.Empty
            .Add(CreateEntry(1, "first"))
            .Add(CreateEntry(2, "second"))
            .SetRotation(SpellQueueRotation.RoundRobin);
        var second = SpellQueueState.Empty
            .Add(CreateEntry(1, "first"))
            .Add(CreateEntry(2, "second"))
            .SetRotation(SpellQueueRotation.RoundRobin);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(second));
            Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
        });
    }

    private static SpellQueueEntry CreateEntry(long id, string name) =>
        new(new SpellQueueEntryId(id), name);

    private static Dictionary<
        SpellQueueEntryId,
        SpellQueueAvailability> Ready(
        params SpellQueueEntry[] entries) =>
        entries.ToDictionary(
            entry => entry.Id,
            _ => SpellQueueAvailability.Ready);
}
