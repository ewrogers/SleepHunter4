using SleepHunter.Runtime.Automation;
using SleepHunter.Runtime.Automation.Panels;
using SleepHunter.Runtime.Automation.Spells;
using SleepHunter.Runtime.Commands;
using SleepHunter.Runtime.Engine;
using SleepHunter.Runtime.Events;
using SleepHunter.Runtime.Intents;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Tests.Scenarios;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Tests.Engine;

public sealed class SpellCastingScenarioTests
{
    private static readonly SpellCastTimingPolicy TestTiming = new(
        TimeSpan.FromMilliseconds(5),
        TimeSpan.FromMilliseconds(10),
        TimeSpan.FromMilliseconds(10),
        TimeSpan.FromMilliseconds(1));

    private static readonly PanelTransitionPolicy TestPanelPolicy = new(
        TimeSpan.FromMilliseconds(50),
        maximumAttempts: 2);

    private static readonly SpellExecutionPolicy TestPolicy = new(
        new SpellCastPolicy(requireMana: true, TestTiming),
        TestPanelPolicy);

    [Test]
    public void ShouldCastFromActivePanelAndRecordCooldownAtDeadline()
    {
        var target = SpellTarget.Self;
        var entry = new SpellQueueEntry(
            new SpellQueueEntryId(1),
            "spell",
            targetLevel: null,
            target);
        var spell = Spell(
            "spell",
            slot: 1,
            castLines: 2,
            cooldown: TimeSpan.FromSeconds(5));
        var scenario = CreateRunningScenario(
            ClientPanel.TemuairSpells,
            entry,
            spell);

        var requested = scenario.Send(new CastNextSpellCommand(TestPolicy));
        var intent = requested.Intent as CastSpellIntent;
        var deadline = requested.ScheduledEvents.Single();
        scenario.AdvanceBy(
            requested.State.SpellCast!.Plan.CastDuration!.Value -
            TimeSpan.FromTicks(1));
        var early = scenario.Dispatch(deadline.Input);
        scenario.AdvanceBy(TimeSpan.FromTicks(1));
        var completed = scenario.Dispatch(deadline.Input);

        Assert.Multiple(() =>
        {
            Assert.That(intent, Is.Not.Null);
            Assert.That(intent!.ActionId.Value, Is.EqualTo(1));
            Assert.That(intent.SpellName, Is.EqualTo(spell.Name));
            Assert.That(intent.Slot, Is.EqualTo(spell.Slot));
            Assert.That(intent.Panel, Is.EqualTo(spell.Panel));
            Assert.That(intent.Target, Is.EqualTo(target));
            Assert.That(
                requested.State.PendingAction?.Deadline.Elapsed,
                Is.EqualTo(TimeSpan.FromMilliseconds(21)));
            Assert.That(
                requested.State.SpellCast?.Status,
                Is.EqualTo(SpellCastStatus.Casting));
            Assert.That(early.State, Is.SameAs(requested.State));
            Assert.That(completed.State.PendingAction, Is.Null);
            Assert.That(
                completed.State.SpellCast?.Status,
                Is.EqualTo(SpellCastStatus.Succeeded));
            Assert.That(
                completed.State.SpellCooldowns.GetReadyAt(
                    spell.Name,
                    scenario.CurrentTime),
                Is.EqualTo(
                    new MacroTimestamp(TimeSpan.FromMilliseconds(5021))));
        });
    }

    [Test]
    public void ShouldResolveCharacterTargetBeforeIssuingCast()
    {
        var sourceLocation = new MapLocationSnapshot(1, "Mileth", 50, 60);
        var targetLocation = new MapLocationSnapshot(1, "Mileth", 53, 58);
        var characterTarget =
            SpellTarget.Character("Alt", new TargetOffset(4, -5));
        var scenario = CreateRunningScenario(
            ClientPanel.TemuairSpells,
            Entry("spell", characterTarget),
            Spell("spell", slot: 1),
            location: sourceLocation);
        scenario.ObserveClientRoster(
            sequence: 1,
            [
                Client(scenario.Client, "Caster", sourceLocation),
                Client(
                    new ClientIdentity("alt-client", "test"),
                    "Alt",
                    targetLocation)
            ]);

        var decision = scenario.Send(new CastNextSpellCommand(TestPolicy));
        var intent = (CastSpellIntent)decision.Intent!;

        Assert.Multiple(() =>
        {
            Assert.That(
                intent.Target,
                Is.EqualTo(
                    SpellTarget.RelativeTile(
                        3,
                        -2,
                        new TargetOffset(4, -5))));
            Assert.That(
                decision.State.SpellCast?.ResolvedTarget,
                Is.EqualTo(intent.Target));
            Assert.That(
                decision.State.SpellCast?.TargetStatus,
                Is.EqualTo(TargetLocationStatus.Resolved));
            Assert.That(
                decision.State.SpellQueue.Entries.Single().Target,
                Is.EqualTo(characterTarget));
        });
    }

    [Test]
    public void ShouldRetryCharacterTargetWithoutConsumingActionId()
    {
        var sourceLocation = new MapLocationSnapshot(1, "Mileth", 50, 60);
        var scenario = CreateRunningScenario(
            ClientPanel.TemuairSpells,
            Entry("spell", SpellTarget.Character("Alt")),
            Spell("spell", slot: 1),
            location: sourceLocation);

        var unavailable = scenario.Send(
            new CastNextSpellCommand(TestPolicy));
        scenario.ObserveClientRoster(
            sequence: 1,
            [
                Client(scenario.Client, "Caster", sourceLocation),
                Client(
                    new ClientIdentity("alt-client", "test"),
                    "Alt",
                    new MapLocationSnapshot(1, "Mileth", 51, 60))
            ]);
        var retried = scenario.Send(new CastNextSpellCommand(TestPolicy));
        var intent = (CastSpellIntent)retried.Intent!;

        Assert.Multiple(() =>
        {
            Assert.That(unavailable.Intent, Is.Null);
            Assert.That(unavailable.State.PendingAction, Is.Null);
            Assert.That(
                unavailable.State.SpellCast?.Status,
                Is.EqualTo(SpellCastStatus.TargetUnavailable));
            Assert.That(
                unavailable.State.SpellCast?.TargetStatus,
                Is.EqualTo(TargetLocationStatus.RosterUnavailable));
            Assert.That(intent.ActionId.Value, Is.EqualTo(1));
            Assert.That(
                intent.Target,
                Is.EqualTo(SpellTarget.RelativeTile(1, 0)));
        });
    }

    [Test]
    public void ShouldRequireFreshSnapshotAndHonorExactCooldownBoundary()
    {
        var entry = Entry("spell");
        var spell = Spell(
            "spell",
            slot: 1,
            castLines: 1,
            cooldown: TimeSpan.FromMilliseconds(100));
        var scenario = CreateRunningScenario(
            ClientPanel.TemuairSpells,
            entry,
            spell);
        var first = scenario.Send(new CastNextSpellCommand(TestPolicy));
        scenario.AdvanceBy(first.State.SpellCast!.Plan.CastDuration!.Value);
        scenario.Dispatch(first.ScheduledEvents.Single().Input);

        var stale = scenario.Send(new CastNextSpellCommand(TestPolicy));
        scenario.AdvanceBy(TimeSpan.FromTicks(1));
        scenario.Observe(
            sequence: 2,
            activePanel: ClientPanel.TemuairSpells,
            vitals: Vitals(),
            spellbook: Spellbook(spell));
        var cooling = scenario.Send(new CastNextSpellCommand(TestPolicy));
        var readyAt = cooling.State.SpellCooldowns.GetReadyAt(
            spell.Name,
            scenario.CurrentTime)!.Value;
        scenario.AdvanceBy(
            readyAt.Elapsed - scenario.CurrentTime.Elapsed);
        scenario.Observe(
            sequence: 3,
            activePanel: ClientPanel.TemuairSpells,
            vitals: Vitals(),
            spellbook: Spellbook(spell));
        var ready = scenario.Send(new CastNextSpellCommand(TestPolicy));

        Assert.Multiple(() =>
        {
            Assert.That(
                stale.State.SpellCast?.Status,
                Is.EqualTo(SpellCastStatus.SnapshotUnavailable));
            Assert.That(stale.Intent, Is.Null);
            Assert.That(
                cooling.State.SpellCast?.Status,
                Is.EqualTo(SpellCastStatus.CoolingDown));
            Assert.That(cooling.Intent, Is.Null);
            Assert.That(ready.Intent, Is.TypeOf<CastSpellIntent>());
            Assert.That(
                ((CastSpellIntent)ready.Intent!).ActionId.Value,
                Is.EqualTo(2));
            Assert.That(ready.State.SpellCooldowns, Is.EqualTo(
                SpellCooldownState.Empty));
        });
    }

    [Test]
    public void ShouldConfirmSpellPanelBeforeCasting()
    {
        var entry = Entry("spell", SpellTarget.Self);
        var spell = Spell("spell", slot: 37, castLines: 1);
        var scenario = CreateRunningScenario(
            ClientPanel.Stats,
            entry,
            spell);

        var requested = scenario.Send(new CastNextSpellCommand(TestPolicy));
        scenario.AdvanceBy(TimeSpan.FromTicks(1));
        var confirmed = scenario.Observe(
            sequence: 2,
            activePanel: ClientPanel.MedeniaSpells,
            vitals: Vitals(),
            spellbook: Spellbook(spell));

        Assert.Multiple(() =>
        {
            Assert.That(requested.Intent, Is.TypeOf<SwitchPanelIntent>());
            Assert.That(
                ((SwitchPanelIntent)requested.Intent!).TargetPanel,
                Is.EqualTo(ClientPanel.MedeniaSpells));
            Assert.That(
                requested.State.SpellCast?.Status,
                Is.EqualTo(SpellCastStatus.WaitingForPanel));
            Assert.That(confirmed.Intent, Is.TypeOf<CastSpellIntent>());
            Assert.That(
                ((CastSpellIntent)confirmed.Intent!).ActionId.Value,
                Is.EqualTo(2));
            Assert.That(
                confirmed.State.PanelTransition?.Status,
                Is.EqualTo(PanelTransitionStatus.Succeeded));
            Assert.That(
                confirmed.State.SpellCast?.Status,
                Is.EqualTo(SpellCastStatus.Casting));
        });
    }

    [Test]
    public void ShouldRevalidateManaAfterPanelConfirmation()
    {
        var entry = Entry("spell");
        var spell = Spell("spell", slot: 73, manaCost: 50);
        var scenario = CreateRunningScenario(
            ClientPanel.Stats,
            entry,
            spell,
            mana: 100);

        scenario.Send(new CastNextSpellCommand(TestPolicy));
        scenario.AdvanceBy(TimeSpan.FromTicks(1));
        var confirmed = scenario.Observe(
            sequence: 2,
            activePanel: ClientPanel.WorldSpells,
            vitals: Vitals(mana: 49),
            spellbook: Spellbook(spell));

        Assert.Multiple(() =>
        {
            Assert.That(confirmed.Intent, Is.Null);
            Assert.That(confirmed.State.PendingAction, Is.Null);
            Assert.That(
                confirmed.State.PanelTransition?.Status,
                Is.EqualTo(PanelTransitionStatus.Succeeded));
            Assert.That(
                confirmed.State.SpellCast?.Status,
                Is.EqualTo(SpellCastStatus.WaitingForMana));
        });
    }

    [Test]
    public void ShouldRevalidateHealthAfterPanelConfirmation()
    {
        var entry = Entry(
            "spell",
            healthCondition: new HealthCondition(
                minimumPercentExclusive: 90));
        var spell = Spell("spell", slot: 73);
        var scenario = new MacroScenario();
        scenario.Send(new AddSpellQueueEntryCommand(entry));
        scenario.Observe(
            sequence: 1,
            activePanel: ClientPanel.Stats,
            vitals: Vitals(health: 91),
            spellbook: Spellbook(spell));
        scenario.Start();

        scenario.Send(new CastNextSpellCommand(TestPolicy));
        scenario.AdvanceBy(TimeSpan.FromTicks(1));
        var confirmed = scenario.Observe(
            sequence: 2,
            activePanel: ClientPanel.WorldSpells,
            vitals: Vitals(health: 90),
            spellbook: Spellbook(spell));

        Assert.Multiple(() =>
        {
            Assert.That(confirmed.Intent, Is.Null);
            Assert.That(confirmed.State.PendingAction, Is.Null);
            Assert.That(
                confirmed.State.PanelTransition?.Status,
                Is.EqualTo(PanelTransitionStatus.Succeeded));
            Assert.That(
                confirmed.State.SpellCast?.Status,
                Is.EqualTo(SpellCastStatus.WaitingForHealth));
        });
    }

    [Test]
    public void ShouldInvalidateSelectionWhenQueueChangesDuringPanelTransition()
    {
        var first = Entry("first", id: 1);
        var second = Entry("second", id: 2);
        var firstSpell = Spell("first", slot: 1);
        var secondSpell = Spell("second", slot: 2);
        var scenario = CreateRunningScenario(
            ClientPanel.Stats,
            first,
            firstSpell);
        scenario.Send(
            new AddSpellQueueEntryCommand(second));
        scenario.Send(new CastNextSpellCommand(TestPolicy));
        scenario.Send(new RemoveSpellQueueEntryCommand(first.Id));
        scenario.AdvanceBy(TimeSpan.FromTicks(1));

        var confirmed = scenario.Observe(
            sequence: 2,
            activePanel: ClientPanel.TemuairSpells,
            vitals: Vitals(),
            spellbook: new SpellbookSnapshot([firstSpell, secondSpell]));

        Assert.Multiple(() =>
        {
            Assert.That(confirmed.Intent, Is.Null);
            Assert.That(confirmed.State.PendingAction, Is.Null);
            Assert.That(
                confirmed.State.SpellCast?.Status,
                Is.EqualTo(SpellCastStatus.SelectionInvalidated));
            Assert.That(
                confirmed.State.SpellQueue.Entries.Single(),
                Is.EqualTo(second));
        });
    }

    [Test]
    public void ShouldRetryPanelThenFailWithoutAdvancingRoundRobin()
    {
        var first = Entry("first", id: 1);
        var second = Entry("second", id: 2);
        var scenario = CreateRunningScenario(
            ClientPanel.Stats,
            first,
            Spell("first", slot: 1));
        scenario.Send(new AddSpellQueueEntryCommand(second));
        scenario.Send(
            new SetSpellQueueRotationCommand(
                SpellQueueRotation.RoundRobin));
        var requested = scenario.Send(new CastNextSpellCommand(TestPolicy));

        scenario.AdvanceBy(TestPanelPolicy.AttemptTimeout);
        var retry = scenario.Dispatch(requested.ScheduledEvents.Single().Input);
        scenario.AdvanceBy(TestPanelPolicy.AttemptTimeout);
        var failed = scenario.Dispatch(retry.ScheduledEvents.Single().Input);

        Assert.Multiple(() =>
        {
            Assert.That(retry.Intent, Is.TypeOf<SwitchPanelIntent>());
            Assert.That(
                retry.State.SpellCast?.Status,
                Is.EqualTo(SpellCastStatus.WaitingForPanel));
            Assert.That(failed.State.PendingAction, Is.Null);
            Assert.That(
                failed.State.SpellCast?.Status,
                Is.EqualTo(SpellCastStatus.PanelUnavailable));
            Assert.That(failed.State.SpellQueue.Cursor, Is.Zero);
        });
    }

    [Test]
    public void ShouldNotSupersedePanelOwnedBySpellCast()
    {
        var scenario = CreateRunningScenario(
            ClientPanel.Stats,
            Entry("spell"),
            Spell("spell", slot: 1));
        var casting = scenario.Send(new CastNextSpellCommand(TestPolicy));

        var manual = scenario.Send(
            new RequestPanelTransitionCommand(
                ClientPanel.Inventory,
                TestPanelPolicy));

        Assert.Multiple(() =>
        {
            Assert.That(manual.State, Is.SameAs(casting.State));
            Assert.That(manual.Intent, Is.Null);
            Assert.That(manual.PublishedView, Is.Null);
            Assert.That(
                ((SwitchPanelIntent)casting.Intent!).TargetPanel,
                Is.EqualTo(ClientPanel.TemuairSpells));
        });
    }

    [Test]
    public void ShouldAdvanceRoundRobinOnlyWhenCastIsIssued()
    {
        var first = Entry("first", id: 1);
        var second = Entry("second", id: 2);
        var firstSpell = Spell("first", slot: 1);
        var secondSpell = Spell("second", slot: 2);
        var scenario = CreateRunningScenario(
            ClientPanel.Stats,
            first,
            firstSpell);
        scenario.Send(new AddSpellQueueEntryCommand(second));
        scenario.Send(
            new SetSpellQueueRotationCommand(
                SpellQueueRotation.RoundRobin));

        var waiting = scenario.Send(new CastNextSpellCommand(TestPolicy));
        scenario.AdvanceBy(TimeSpan.FromTicks(1));
        var casting = scenario.Observe(
            sequence: 2,
            activePanel: ClientPanel.TemuairSpells,
            vitals: Vitals(),
            spellbook: new SpellbookSnapshot([firstSpell, secondSpell]));

        Assert.Multiple(() =>
        {
            Assert.That(waiting.State.SpellQueue.Cursor, Is.Zero);
            Assert.That(casting.Intent, Is.TypeOf<CastSpellIntent>());
            Assert.That(casting.State.SpellQueue.Cursor, Is.EqualTo(1));
        });
    }

    [Test]
    public void ShouldAdvanceAreaTargetOnlyWhenCastIsIssued()
    {
        var area = SpellTarget.RelativeArea(0, 0, 0, 1);
        var entry = Entry("spell", area);
        var spell = Spell("spell", slot: 1);
        var scenario = CreateRunningScenario(
            ClientPanel.Stats,
            entry,
            spell);

        var waiting = scenario.Send(new CastNextSpellCommand(TestPolicy));
        scenario.AdvanceBy(TimeSpan.FromTicks(1));
        var casting = scenario.Observe(
            sequence: 2,
            activePanel: ClientPanel.TemuairSpells,
            vitals: Vitals(),
            spellbook: Spellbook(spell));

        Assert.Multiple(() =>
        {
            Assert.That(waiting.Intent, Is.TypeOf<SwitchPanelIntent>());
            Assert.That(waiting.State.SpellTargetRotations.GetCursor(1), Is.Zero);
            Assert.That(
                ((CastSpellIntent)casting.Intent!).Target,
                Is.EqualTo(SpellTarget.RelativeTile(0, 0)));
            Assert.That(
                casting.State.SpellTargetRotations.GetCursor(1),
                Is.EqualTo(1));
            Assert.That(
                casting.State.SpellCast?.ResolvedTarget,
                Is.EqualTo(SpellTarget.RelativeTile(0, 0)));
        });
    }

    [Test]
    public void ShouldResolveNextAreaPointOnNextCast()
    {
        var area = SpellTarget.RelativeArea(0, 0, 0, 1);
        var spell = Spell("spell", slot: 1);
        var scenario = CreateRunningScenario(
            ClientPanel.TemuairSpells,
            Entry("spell", area),
            spell);
        var first = scenario.Send(new CastNextSpellCommand(TestPolicy));
        scenario.AdvanceBy(first.State.SpellCast!.CastDuration!.Value);
        scenario.Dispatch(first.ScheduledEvents.Single().Input);
        scenario.AdvanceBy(TimeSpan.FromTicks(1));
        scenario.Observe(
            sequence: 2,
            activePanel: ClientPanel.TemuairSpells,
            vitals: Vitals(),
            spellbook: Spellbook(spell));

        var second = scenario.Send(new CastNextSpellCommand(TestPolicy));

        Assert.Multiple(() =>
        {
            Assert.That(
                ((CastSpellIntent)first.Intent!).Target,
                Is.EqualTo(SpellTarget.RelativeTile(0, 0)));
            Assert.That(
                ((CastSpellIntent)second.Intent!).Target,
                Is.EqualTo(SpellTarget.RelativeTile(0, -1)));
            Assert.That(
                second.State.SpellTargetRotations.GetCursor(1),
                Is.EqualTo(2));
        });
    }

    [Test]
    public void ShouldInvalidateSelectionWhenSpellSlotChangesDuringPanelTransition()
    {
        var entry = Entry("spell");
        var original = Spell("spell", slot: 1);
        var moved = Spell("spell", slot: 37);
        var scenario = CreateRunningScenario(
            ClientPanel.Stats,
            entry,
            original);
        scenario.Send(new CastNextSpellCommand(TestPolicy));
        scenario.AdvanceBy(TimeSpan.FromTicks(1));

        var confirmed = scenario.Observe(
            sequence: 2,
            activePanel: ClientPanel.TemuairSpells,
            vitals: Vitals(),
            spellbook: Spellbook(moved));

        Assert.Multiple(() =>
        {
            Assert.That(confirmed.Intent, Is.Null);
            Assert.That(confirmed.State.PendingAction, Is.Null);
            Assert.That(
                confirmed.State.SpellCast?.Status,
                Is.EqualTo(SpellCastStatus.SelectionInvalidated));
        });
    }

    [Test]
    public void ShouldPreserveSpellCooldownDuringUnrelatedPanelRetry()
    {
        var spell = Spell(
            "spell",
            slot: 1,
            cooldown: TimeSpan.FromSeconds(5));
        var scenario = CreateRunningScenario(
            ClientPanel.TemuairSpells,
            Entry(spell.Name),
            spell);
        var cast = scenario.Send(new CastNextSpellCommand(TestPolicy));
        scenario.AdvanceBy(cast.State.SpellCast!.Plan.CastDuration!.Value);
        scenario.Dispatch(cast.ScheduledEvents.Single().Input);
        var readyAt = scenario.State.SpellCooldowns.GetReadyAt(
            spell.Name,
            scenario.CurrentTime);
        var panel = scenario.Send(
            new RequestPanelTransitionCommand(
                ClientPanel.Inventory,
                TestPanelPolicy));

        scenario.AdvanceBy(TestPanelPolicy.AttemptTimeout);
        var retry = scenario.Dispatch(panel.ScheduledEvents.Single().Input);

        Assert.Multiple(() =>
        {
            Assert.That(retry.Intent, Is.TypeOf<SwitchPanelIntent>());
            Assert.That(
                retry.State.SpellCooldowns.GetReadyAt(
                    spell.Name,
                    scenario.CurrentTime),
                Is.EqualTo(readyAt));
        });
    }

    [Test]
    public void ShouldCancelCastAndIgnoreStaleDeadlineWhenPaused()
    {
        var scenario = CreateRunningScenario(
            ClientPanel.TemuairSpells,
            Entry("spell"),
            Spell("spell", slot: 1));
        var requested = scenario.Send(new CastNextSpellCommand(TestPolicy));

        var paused = scenario.Pause();
        scenario.AdvanceBy(requested.State.SpellCast!.Plan.CastDuration!.Value);
        var stale = scenario.Dispatch(requested.ScheduledEvents.Single().Input);

        Assert.Multiple(() =>
        {
            Assert.That(paused.State.PendingAction, Is.Null);
            Assert.That(
                paused.State.SpellCast?.Status,
                Is.EqualTo(SpellCastStatus.Cancelled));
            Assert.That(stale.State, Is.SameAs(paused.State));
            Assert.That(stale.PublishedView, Is.Null);
        });
    }

    [Test]
    public void ShouldCancelWaitingCastWhenStoppedOrLoggedOut()
    {
        var stoppedScenario = CreateRunningScenario(
            ClientPanel.Stats,
            Entry("spell"),
            Spell("spell", slot: 1));
        stoppedScenario.Send(new CastNextSpellCommand(TestPolicy));
        var stopped = stoppedScenario.Stop();

        var loggedOutScenario = CreateRunningScenario(
            ClientPanel.TemuairSpells,
            Entry("spell"),
            Spell("spell", slot: 1));
        loggedOutScenario.Send(new CastNextSpellCommand(TestPolicy));
        loggedOutScenario.AdvanceBy(TimeSpan.FromTicks(1));
        var loggedOut = loggedOutScenario.Observe(
            sequence: 2,
            presence: ClientPresence.LoggedOut);

        Assert.Multiple(() =>
        {
            Assert.That(
                stopped.State.SpellCast?.Status,
                Is.EqualTo(SpellCastStatus.Cancelled));
            Assert.That(stopped.State.PendingAction, Is.Null);
            Assert.That(
                loggedOut.State.SpellCast?.Status,
                Is.EqualTo(SpellCastStatus.Cancelled));
            Assert.That(loggedOut.State.PendingAction, Is.Null);
            Assert.That(
                loggedOut.State.StopReason,
                Is.EqualTo(MacroStopReason.ClientLoggedOut));
        });
    }

    [Test]
    public void ShouldExposeStablePlanningOutcomesWithoutIntent()
    {
        var empty = new MacroScenario();
        empty.Observe(
            sequence: 1,
            activePanel: ClientPanel.TemuairSpells,
            vitals: Vitals(),
            spellbook: SpellbookSnapshot.Empty);
        empty.Start();
        var emptyDecision = empty.Send(new CastNextSpellCommand(TestPolicy));

        var missing = new MacroScenario();
        missing.Send(new AddSpellQueueEntryCommand(Entry("spell")));
        missing.Observe(
            sequence: 1,
            activePanel: ClientPanel.TemuairSpells);
        missing.Start();
        var missingDecision = missing.Send(
            new CastNextSpellCommand(TestPolicy));

        Assert.Multiple(() =>
        {
            Assert.That(
                emptyDecision.State.SpellCast?.Status,
                Is.EqualTo(SpellCastStatus.QueueEmpty));
            Assert.That(emptyDecision.Intent, Is.Null);
            Assert.That(
                missingDecision.State.SpellCast?.Status,
                Is.EqualTo(SpellCastStatus.SnapshotUnavailable));
            Assert.That(missingDecision.Intent, Is.Null);
        });
    }

    private static MacroScenario CreateRunningScenario(
        ClientPanel activePanel,
        SpellQueueEntry entry,
        SpellSnapshot spell,
        int mana = 100,
        MapLocationSnapshot? location = null)
    {
        var scenario = new MacroScenario();
        scenario.Send(new AddSpellQueueEntryCommand(entry));
        scenario.Observe(
            sequence: 1,
            activePanel: activePanel,
            vitals: Vitals(mana),
            spellbook: Spellbook(spell),
            location: location);
        scenario.Start();
        return scenario;
    }

    private static SpellQueueEntry Entry(
        string name,
        SpellTarget? target = null,
        long id = 1,
        HealthCondition? healthCondition = null) =>
        new(
            new SpellQueueEntryId(id),
            name,
            targetLevel: null,
            target,
            healthCondition);

    private static SpellSnapshot Spell(
        string name,
        int slot,
        int castLines = 1,
        int manaCost = 0,
        TimeSpan? cooldown = null) =>
        new(
            name,
            slot,
            currentLevel: 0,
            maximumLevel: 100,
            castLines,
            manaCost,
            cooldown ?? TimeSpan.Zero);

    private static VitalsSnapshot Vitals(
        int mana = 100,
        int health = 100) =>
        new(
            currentHealth: health,
            maximumHealth: 100,
            currentMana: mana,
            maximumMana: 100);

    private static SpellbookSnapshot Spellbook(
        params SpellSnapshot[] spells) =>
        new(spells);

    private static ClientRosterEntry Client(
        ClientIdentity client,
        string characterName,
        MapLocationSnapshot location) =>
        new(
            client,
            characterName,
            ClientPresence.InWorld,
            isMacroRunning: true,
            isWaitingForMana: false,
            location,
            vitals: null);
}
