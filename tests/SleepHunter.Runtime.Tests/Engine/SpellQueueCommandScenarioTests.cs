using SleepHunter.Runtime.Automation.Spells;
using SleepHunter.Runtime.Commands;
using SleepHunter.Runtime.Engine;
using SleepHunter.Runtime.Tests.Scenarios;

namespace SleepHunter.Runtime.Tests.Engine;

public sealed class SpellQueueCommandScenarioTests
{
    [Test]
    public void ShouldApplyQueueEditsInCommandOrder()
    {
        var scenario = new MacroScenario();
        var first = CreateEntry(1, "first");
        var updatedFirst = new SpellQueueEntry(
            first.Id,
            first.Name,
            targetLevel: 10);
        var second = CreateEntry(2, "second");

        scenario.Send(new AddSpellQueueEntryCommand(first));
        scenario.Send(new AddSpellQueueEntryCommand(second));
        scenario.Send(new MoveSpellQueueEntryCommand(second.Id, targetIndex: 0));
        scenario.Send(new UpdateSpellQueueEntryCommand(updatedFirst));
        scenario.Send(
            new SetSpellQueueRotationCommand(SpellQueueRotation.RoundRobin));
        scenario.Send(new RemoveSpellQueueEntryCommand(second.Id));

        var finalQueue = scenario.State.SpellQueue;

        Assert.Multiple(() =>
        {
            Assert.That(scenario.State.Revision, Is.EqualTo(6));
            Assert.That(finalQueue.Entries, Has.Length.EqualTo(1));
            Assert.That(finalQueue.Entries[0], Is.EqualTo(updatedFirst));
            Assert.That(
                finalQueue.Rotation,
                Is.EqualTo(SpellQueueRotation.RoundRobin));
            Assert.That(
                scenario.Decisions
                    .Select(decision => decision.PublishedView)
                    .OfType<MacroViewSnapshot>()
                    .Select(view => view.Revision),
                Is.EqualTo(new long[] { 1, 2, 3, 4, 5, 6 }));
        });
    }

    [Test]
    public void ShouldIgnoreQueueCommandsThatDoNotChangeState()
    {
        var scenario = new MacroScenario();
        var first = CreateEntry(1, "first");
        scenario.Send(new AddSpellQueueEntryCommand(first));
        var acceptedState = scenario.State;

        var duplicate = scenario.Send(new AddSpellQueueEntryCommand(first));
        var invalidInsertion = scenario.Send(
            new AddSpellQueueEntryCommand(
                CreateEntry(2, "second"),
                index: 5));
        var missingRemoval = scenario.Send(
            new RemoveSpellQueueEntryCommand(new SpellQueueEntryId(2)));
        var invalidMove = scenario.Send(
            new MoveSpellQueueEntryCommand(first.Id, targetIndex: 5));
        var sameRotation = scenario.Send(
            new SetSpellQueueRotationCommand(SpellQueueRotation.Priority));

        Assert.Multiple(() =>
        {
            Assert.That(duplicate.State, Is.SameAs(acceptedState));
            Assert.That(invalidInsertion.State, Is.SameAs(acceptedState));
            Assert.That(missingRemoval.State, Is.SameAs(acceptedState));
            Assert.That(invalidMove.State, Is.SameAs(acceptedState));
            Assert.That(sameRotation.State, Is.SameAs(acceptedState));
            Assert.That(duplicate.PublishedView, Is.Null);
            Assert.That(invalidInsertion.PublishedView, Is.Null);
            Assert.That(missingRemoval.PublishedView, Is.Null);
            Assert.That(invalidMove.PublishedView, Is.Null);
            Assert.That(sameRotation.PublishedView, Is.Null);
        });
    }

    [Test]
    public void ShouldPreserveRunningLifecycleDuringQueueEdits()
    {
        var scenario = new MacroScenario();
        scenario.Observe(sequence: 1);
        scenario.Start();

        var edited = scenario.Send(
            new AddSpellQueueEntryCommand(CreateEntry(1, "first")));

        Assert.Multiple(() =>
        {
            Assert.That(
                edited.State.Lifecycle,
                Is.EqualTo(MacroLifecycle.Running));
            Assert.That(
                edited.PublishedView?.Lifecycle,
                Is.EqualTo(MacroLifecycle.Running));
            Assert.That(edited.State.SpellQueue.Entries, Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void ShouldPublishEmptyQueueAfterClear()
    {
        var scenario = new MacroScenario();
        scenario.Send(new AddSpellQueueEntryCommand(CreateEntry(1, "first")));

        var cleared = scenario.Send(new ClearSpellQueueCommand());
        var repeatedClear = scenario.Send(new ClearSpellQueueCommand());

        Assert.Multiple(() =>
        {
            Assert.That(cleared.State.SpellQueue.Entries, Is.Empty);
            Assert.That(cleared.State.SpellQueue.Cursor, Is.Zero);
            Assert.That(cleared.PublishedView?.SpellQueue.Entries, Is.Empty);
            Assert.That(repeatedClear.PublishedView, Is.Null);
        });
    }

    [Test]
    public void ShouldProduceEqualDecisionsForEqualQueueCommands()
    {
        var first = RunQueueScenario();
        var second = RunQueueScenario();

        Assert.That(first, Is.EqualTo(second));
    }

    private static MacroDecision[] RunQueueScenario()
    {
        var scenario = new MacroScenario();
        scenario.Send(
            new AddSpellQueueEntryCommand(CreateEntry(1, "first")));
        scenario.Send(
            new AddSpellQueueEntryCommand(CreateEntry(2, "second")));
        scenario.Send(
            new SetSpellQueueRotationCommand(SpellQueueRotation.RoundRobin));
        scenario.Send(
            new MoveSpellQueueEntryCommand(
                new SpellQueueEntryId(2),
                targetIndex: 0));

        return scenario.Decisions.ToArray();
    }

    private static SpellQueueEntry CreateEntry(long id, string name) =>
        new(new SpellQueueEntryId(id), name);
}
