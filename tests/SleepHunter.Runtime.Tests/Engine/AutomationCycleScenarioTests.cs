using SleepHunter.Runtime.Automation;
using SleepHunter.Runtime.Automation.Flowering;
using SleepHunter.Runtime.Automation.Panels;
using SleepHunter.Runtime.Automation.Skills;
using SleepHunter.Runtime.Automation.Spells;
using SleepHunter.Runtime.Commands;
using SleepHunter.Runtime.Engine;
using SleepHunter.Runtime.Events;
using SleepHunter.Runtime.Hosting;
using SleepHunter.Runtime.Intents;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Tests.Hosting;
using SleepHunter.Runtime.Tests.Scenarios;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Tests.Engine;

public sealed class AutomationCycleScenarioTests
{
    private static readonly SpellCastTimingPolicy TestTiming = new(
        zeroLineDuration: TimeSpan.Zero,
        singleLineDuration: TimeSpan.FromMilliseconds(10),
        multiLineDurationPerLine: TimeSpan.FromMilliseconds(10),
        completionPadding: TimeSpan.FromMilliseconds(1));

    private static readonly SpellExecutionPolicy TestSpellPolicy = new(
        new SpellCastPolicy(requireMana: true, TestTiming),
        new PanelTransitionPolicy(
            TimeSpan.FromMilliseconds(50),
            maximumAttempts: 2),
        allowStaffSwitching: false);

    [Test]
    public async Task ShouldRunConfiguredAutomationWithoutActionCommands()
    {
        var timeProvider = new ManualTimeProvider();
        await using var session = new MacroSession(
            new MacroEngine(),
            new MacroClock(timeProvider));
        var entry = SpellEntry();
        var spell = Spell(entry.Name, slot: 1);

        session.PublishSnapshot(
            Snapshot(
                sequence: 1,
                MacroTimestamp.Zero,
                ClientPanel.TemuairSpells,
                spells: [spell]));
        await session.Views.ReadUntilAsync(
            view => view.LatestSnapshotSequence?.Value == 1);
        await session.SendCommandAsync(
            new AddSpellQueueEntryCommand(entry));
        await session.Views.ReadUntilAsync(
            view => view.SpellQueue.Entries.Length == 1);
        var configuration = new AutomationConfiguration(
            spellsEnabled: true,
            spellPolicy: TestSpellPolicy);
        await session.SendCommandAsync(
            new ConfigureAutomationCommand(configuration));
        await session.Views.ReadUntilAsync(
            view => view.Automation == configuration);

        await session.SendCommandAsync(new StartMacroCommand());

        var intent = await session.Intents.ReadUntilAsync(
            value => value is CastSpellIntent);
        Assert.Multiple(() =>
        {
            Assert.That(intent, Is.TypeOf<CastSpellIntent>());
            Assert.That(
                ((CastSpellIntent)intent).SpellName,
                Is.EqualTo(entry.Name));
        });
    }

    [Test]
    public void ShouldRemainInertUntilAutomationIsConfigured()
    {
        var scenario = new MacroScenario(issueActions: false);
        var entry = SpellEntry();
        scenario.Observe(
            sequence: 1,
            activePanel: ClientPanel.TemuairSpells,
            vitals: Vitals(),
            spellbook: new SpellbookSnapshot(
                [Spell(entry.Name, slot: 1)]));
        scenario.Send(new AddSpellQueueEntryCommand(entry));

        var started = scenario.Start();

        Assert.Multiple(() =>
        {
            Assert.That(started.RaisedEvents, Is.Empty);
            Assert.That(started.Intent, Is.Null);
            Assert.That(
                started.State.Automation,
                Is.EqualTo(AutomationConfiguration.Disabled));
        });
    }

    [Test]
    public void ShouldStartAutomationWhenEnabledWhileRunning()
    {
        var scenario = new MacroScenario(issueActions: false);
        var entry = SpellEntry();
        scenario.Observe(
            sequence: 1,
            activePanel: ClientPanel.TemuairSpells,
            vitals: Vitals(),
            spellbook: new SpellbookSnapshot(
                [Spell(entry.Name, slot: 1)]));
        scenario.Send(new AddSpellQueueEntryCommand(entry));
        scenario.Start();

        var configured = scenario.Send(
            new ConfigureAutomationCommand(
                new AutomationConfiguration(
                    spellsEnabled: true,
                    spellPolicy: TestSpellPolicy)));
        var cycle = scenario.Dispatch(
            configured.RaisedEvents.Single());

        Assert.Multiple(() =>
        {
            Assert.That(
                configured.State.Lifecycle,
                Is.EqualTo(MacroLifecycle.Running));
            Assert.That(
                configured.RaisedEvents.Single(),
                Is.TypeOf<AutomationCycleRequested>());
            Assert.That(cycle.Intent, Is.TypeOf<CastSpellIntent>());
        });
    }

    [Test]
    public void ShouldWaitUntilTheUserStopsChatting()
    {
        var scenario = new MacroScenario(issueActions: false);
        var entry = SpellEntry();
        var spellbook = new SpellbookSnapshot(
            [Spell(entry.Name, slot: 1)]);
        scenario.Observe(
            sequence: 1,
            activePanel: ClientPanel.TemuairSpells,
            vitals: Vitals(),
            spellbook: spellbook,
            isChatOpen: true);
        scenario.Send(new AddSpellQueueEntryCommand(entry));
        scenario.Send(
            new ConfigureAutomationCommand(
                new AutomationConfiguration(
                    spellsEnabled: true,
                    spellPolicy: TestSpellPolicy)));
        var started = scenario.Start();

        var blocked = scenario.Dispatch(
            started.RaisedEvents.Single());
        scenario.AdvanceBy(TimeSpan.FromTicks(1));
        var observed = scenario.Observe(
            sequence: 2,
            activePanel: ClientPanel.TemuairSpells,
            vitals: Vitals(),
            spellbook: spellbook,
            isChatOpen: false);
        var resumed = scenario.Dispatch(
            observed.RaisedEvents.Single());

        Assert.Multiple(() =>
        {
            Assert.That(blocked.State, Is.SameAs(started.State));
            Assert.That(blocked.Intent, Is.Null);
            Assert.That(blocked.PublishedView, Is.Null);
            Assert.That(resumed.Intent, Is.TypeOf<CastSpellIntent>());
        });
    }

    [Test]
    public void ShouldFallThroughUnavailableCategoriesAndIssueOneIntent()
    {
        var scenario = new MacroScenario(issueActions: false);
        var skillEntry = new SkillQueueEntry(
            new SkillQueueEntryId(1),
            "queued skill");
        var skill = new SkillSnapshot(
            skillEntry.Name,
            slot: 1,
            currentLevel: 0,
            maximumLevel: 100,
            manaCost: 0,
            cooldown: TimeSpan.Zero);
        scenario.Observe(
            sequence: 1,
            activePanel: ClientPanel.TemuairSkills,
            vitals: Vitals(),
            skillbook: new SkillbookSnapshot([skill]));
        scenario.Send(new AddSkillQueueEntryCommand(skillEntry));
        scenario.Send(
            new ConfigureAutomationCommand(
                new AutomationConfiguration(
                    spellsEnabled: true,
                    skillsEnabled: true)));
        var started = scenario.Start();

        var cycle = scenario.Dispatch(
            started.RaisedEvents.Single());

        Assert.Multiple(() =>
        {
            Assert.That(cycle.Intent, Is.TypeOf<UseSkillIntent>());
            Assert.That(
                cycle.State.SpellCast?.Status,
                Is.EqualTo(SpellCastStatus.QueueEmpty));
            Assert.That(
                cycle.State.PendingAction?.Intent,
                Is.TypeOf<UseSkillIntent>());
            Assert.That(cycle.ScheduledEvents, Has.Length.EqualTo(1));
            Assert.That(
                cycle.State.Revision,
                Is.EqualTo(started.State.Revision + 1));
        });
    }

    [TestCase(true, FlowerSpellNames.Plant)]
    [TestCase(false, "queued spell")]
    public void ShouldHonorFlowerAndSpellPriority(
        bool flowerBeforeSpells,
        string expectedSpell)
    {
        var scenario = new MacroScenario();
        var spellEntry = SpellEntry();
        var flowerEntry = new FlowerQueueEntry(
            new FlowerQueueEntryId(1),
            SpellTarget.Self,
            interval: TimeSpan.Zero);
        var plant = Spell(FlowerSpellNames.Plant, slot: 1);
        var queuedSpell = Spell(spellEntry.Name, slot: 2);
        scenario.Observe(
            sequence: 1,
            activePanel: ClientPanel.TemuairSpells,
            vitals: Vitals(),
            spellbook: new SpellbookSnapshot(
                [plant, queuedSpell]),
            location: Location());
        scenario.Send(new AddSpellQueueEntryCommand(spellEntry));
        scenario.Send(new AddFlowerQueueEntryCommand(flowerEntry));
        scenario.Send(
            new ConfigureAutomationCommand(
                new AutomationConfiguration(
                    spellsEnabled: true,
                    floweringEnabled: true,
                    flowerBeforeSpells: flowerBeforeSpells,
                    spellPolicy: TestSpellPolicy,
                    flowerPolicy: new FlowerExecutionPolicy(
                        spell: TestSpellPolicy))));
        var started = scenario.Start();

        var cycle = scenario.Dispatch(
            started.RaisedEvents.Single());

        Assert.Multiple(() =>
        {
            Assert.That(cycle.Intent, Is.TypeOf<CastSpellIntent>());
            Assert.That(
                ((CastSpellIntent)cycle.Intent!).SpellName,
                Is.EqualTo(expectedSpell));
            Assert.That(cycle.ScheduledEvents, Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void ShouldContinueSpellsWhenPrioritizedFlowerTargetIsUnavailable()
    {
        var scenario = new MacroScenario();
        var spellEntry = SpellEntry();
        var flowerEntry = new FlowerQueueEntry(
            new FlowerQueueEntryId(1),
            SpellTarget.Character("Monitor"),
            interval: TimeSpan.Zero);
        var plant = Spell(FlowerSpellNames.Plant, slot: 1);
        var queuedSpell = Spell(spellEntry.Name, slot: 2);
        scenario.Observe(
            sequence: 1,
            activePanel: ClientPanel.TemuairSpells,
            vitals: Vitals(),
            spellbook: new SpellbookSnapshot(
                [plant, queuedSpell]),
            location: Location());
        scenario.Send(new AddSpellQueueEntryCommand(spellEntry));
        scenario.Send(new AddFlowerQueueEntryCommand(flowerEntry));
        scenario.ObserveClientRoster(
            sequence: 1,
            clients:
            [
                new ClientRosterEntry(
                    new ClientIdentity("monitor-client"),
                    "Monitor",
                    ClientPresence.InWorld,
                    isMacroRunning: true,
                    isWaitingForMana: false,
                    new MapLocationSnapshot(
                        mapNumber: 1,
                        mapName: "test map",
                        x: 55,
                        y: 55),
                    Vitals())
            ]);
        scenario.Send(
            new ConfigureAutomationCommand(
                new AutomationConfiguration(
                    spellsEnabled: true,
                    floweringEnabled: true,
                    flowerBeforeSpells: true,
                    spellPolicy: TestSpellPolicy,
                    flowerPolicy: new FlowerExecutionPolicy(
                        spell: TestSpellPolicy))));
        var started = scenario.Start();

        var cycle = scenario.Dispatch(
            started.RaisedEvents.Single());

        Assert.Multiple(() =>
        {
            Assert.That(cycle.Intent, Is.TypeOf<CastSpellIntent>());
            Assert.That(
                ((CastSpellIntent)cycle.Intent!).SpellName,
                Is.EqualTo(queuedSpell.Name));
            Assert.That(
                cycle.State.Flower?.Status,
                Is.EqualTo(FlowerStatus.TargetUnavailable));
            Assert.That(
                cycle.State.SpellCast?.Origin,
                Is.EqualTo(SpellCastOrigin.SpellQueue));
            Assert.That(
                cycle.State.SpellCast?.Status,
                Is.EqualTo(SpellCastStatus.Casting));
        });
    }

    [Test]
    public void ShouldContinueAutomaticFloweringAfterLiveQueueRemoval()
    {
        var scenario = new MacroScenario();
        var spellEntry = SpellEntry();
        var flowerEntry = new FlowerQueueEntry(
            new FlowerQueueEntryId(1),
            SpellTarget.Self,
            interval: TimeSpan.Zero);
        var plant = Spell(FlowerSpellNames.Plant, slot: 1);
        var queuedSpell = Spell(spellEntry.Name, slot: 2);
        var flowerPolicy = new FlowerExecutionPolicy(
            target: new FlowerTargetPolicy(
                autoFlowerWaitingCharacters: true),
            spell: TestSpellPolicy);
        var configuration = new AutomationConfiguration(
            spellsEnabled: true,
            floweringEnabled: true,
            flowerBeforeSpells: true,
            spellPolicy: TestSpellPolicy,
            flowerPolicy: flowerPolicy);
        scenario.Observe(
            sequence: 1,
            activePanel: ClientPanel.Stats,
            vitals: Vitals(),
            spellbook: new SpellbookSnapshot(
                [plant, queuedSpell]),
            location: Location());
        scenario.Send(new AddSpellQueueEntryCommand(spellEntry));
        scenario.Send(new AddFlowerQueueEntryCommand(flowerEntry));
        scenario.Send(new ConfigureAutomationCommand(configuration));
        var started = scenario.Start();
        var flowering = scenario.Dispatch(
            started.RaisedEvents.Single());

        var updated = scenario.Send(
            new ApplyAutomationSetupCommand(
                new ReplaceQueuesCommand(
                    [spellEntry],
                    SpellQueueRotation.Priority,
                    skills: [],
                    flowers: []),
                configuration));
        var continued = scenario.Dispatch(
            updated.RaisedEvents.Single());

        Assert.Multiple(() =>
        {
            Assert.That(
                flowering.State.Flower?.Status,
                Is.EqualTo(FlowerStatus.WaitingForPanel));
            Assert.That(
                updated.State.Lifecycle,
                Is.EqualTo(MacroLifecycle.Running));
            Assert.That(updated.State.FlowerQueue.Entries, Is.Empty);
            Assert.That(updated.State.PendingAction, Is.Null);
            Assert.That(
                updated.State.Flower?.Status,
                Is.EqualTo(FlowerStatus.Cancelled));
            Assert.That(
                updated.State.SpellCast?.Status,
                Is.EqualTo(SpellCastStatus.Cancelled));
            Assert.That(
                continued.State.Flower?.Status,
                Is.EqualTo(FlowerStatus.WaitingForTarget));
            Assert.That(
                continued.State.SpellCast?.Origin,
                Is.EqualTo(SpellCastOrigin.SpellQueue));
            Assert.That(
                continued.Intent,
                Is.TypeOf<SwitchPanelIntent>());
        });
    }

    [Test]
    public void ShouldReleaseOrphanedFlowerBeforeSpellFirstAutomation()
    {
        var scenario = new MacroScenario();
        var spellEntry = SpellEntry();
        var flowerEntry = new FlowerQueueEntry(
            new FlowerQueueEntryId(1),
            SpellTarget.Self,
            interval: TimeSpan.Zero);
        var plant = Spell(FlowerSpellNames.Plant, slot: 1);
        var queuedSpell = Spell(spellEntry.Name, slot: 2);
        var flowerPolicy = new FlowerExecutionPolicy(
            target: new FlowerTargetPolicy(
                autoFlowerWaitingCharacters: true),
            spell: TestSpellPolicy);
        var flowerFirst = new AutomationConfiguration(
            spellsEnabled: true,
            floweringEnabled: true,
            flowerBeforeSpells: true,
            spellPolicy: TestSpellPolicy,
            flowerPolicy: flowerPolicy);
        scenario.Observe(
            sequence: 1,
            activePanel: ClientPanel.Stats,
            vitals: Vitals(),
            spellbook: new SpellbookSnapshot(
                [plant, queuedSpell]),
            location: Location());
        scenario.Send(new AddSpellQueueEntryCommand(spellEntry));
        scenario.Send(new AddFlowerQueueEntryCommand(flowerEntry));
        scenario.Send(new ConfigureAutomationCommand(flowerFirst));
        var started = scenario.Start();
        var flowering = scenario.Dispatch(
            started.RaisedEvents.Single());
        var active = flowering.State;
        var spellFirst = new AutomationConfiguration(
            spellsEnabled: true,
            floweringEnabled: true,
            flowerBeforeSpells: false,
            spellPolicy: TestSpellPolicy,
            flowerPolicy: flowerPolicy);
        var orphaned = new MacroState(
            active.Revision,
            active.Lifecycle,
            active.StopReason,
            active.LatestSnapshot,
            active.LastTransitionAt,
            pendingAction: null,
            active.SpellQueue,
            nextClientActionId: active.NextClientActionId,
            spellCooldowns: active.SpellCooldowns,
            spellCast: active.SpellCast!.Cancelled(),
            skillQueue: active.SkillQueue,
            skillCooldowns: active.SkillCooldowns,
            skillUse: active.SkillUse,
            disarm: active.Disarm,
            dialog: active.Dialog,
            flowerQueue: FlowerQueueState.Empty,
            flowerSchedules: FlowerScheduleState.Empty,
            clientRoster: active.ClientRoster,
            flower: active.Flower,
            spellTargetRotations: active.SpellTargetRotations,
            flowerTargetRotations: TargetRotationState.Empty,
            lastActionIssue: active.LastActionIssue,
            automation: spellFirst);

        var continued = new MacroEngine().Decide(
            orphaned,
            new AutomationCycleRequested(),
            scenario.CurrentTime);

        Assert.Multiple(() =>
        {
            Assert.That(
                orphaned.Flower?.Status,
                Is.EqualTo(FlowerStatus.WaitingForPanel));
            Assert.That(
                continued.State.Flower?.Status,
                Is.EqualTo(FlowerStatus.Cancelled));
            Assert.That(
                continued.State.SpellCast?.Origin,
                Is.EqualTo(SpellCastOrigin.SpellQueue));
            Assert.That(
                continued.State.SpellCast?.Status,
                Is.EqualTo(SpellCastStatus.WaitingForPanel));
            Assert.That(
                continued.Intent,
                Is.TypeOf<SwitchPanelIntent>());
        });
    }

    [Test]
    public void ShouldWaitForManaRestorationEffectBeforeRetrying()
    {
        var scenario = new MacroScenario();
        var queuedEntry = SpellEntry();
        var queuedSpell = Spell(
            queuedEntry.Name,
            slot: 1,
            manaCost: 500);
        var restoration = Spell(
            FlowerSpellNames.ManaRestoration,
            slot: 2);
        var spellbook = new SpellbookSnapshot(
            [queuedSpell, restoration]);
        scenario.Observe(
            sequence: 1,
            activePanel: ClientPanel.TemuairSpells,
            vitals: Vitals(
                currentMana: 100,
                maximumMana: 1000),
            spellbook: spellbook);
        scenario.Send(new AddSpellQueueEntryCommand(queuedEntry));
        scenario.Send(
            new ConfigureAutomationCommand(
                new AutomationConfiguration(
                    spellsEnabled: true,
                    spellPolicy: TestSpellPolicy,
                    flowerPolicy: new FlowerExecutionPolicy(
                        spell: TestSpellPolicy,
                        restoreManaOnDemand: true))));
        var started = scenario.Start();
        var restoring = scenario.Dispatch(
            started.RaisedEvents.Single());
        var deadline = restoring.ScheduledEvents.Single();
        scenario.AdvanceBy(
            deadline.DueAt.Elapsed - scenario.CurrentTime.Elapsed);
        var completed = scenario.Dispatch(deadline.Input);

        scenario.AdvanceBy(TimeSpan.FromMilliseconds(500));
        var unchangedMana = scenario.Observe(
            sequence: 2,
            activePanel: ClientPanel.TemuairSpells,
            captureStartedAt: scenario.CurrentTime,
            captureCompletedAt: scenario.CurrentTime,
            vitals: Vitals(
                currentMana: 100,
                maximumMana: 1000),
            spellbook: spellbook);
        var waiting = scenario.Dispatch(
            unchangedMana.RaisedEvents.Single());

        scenario.AdvanceBy(TimeSpan.FromMilliseconds(500));
        var restoredMana = scenario.Observe(
            sequence: 3,
            activePanel: ClientPanel.TemuairSpells,
            captureStartedAt: scenario.CurrentTime,
            captureCompletedAt: scenario.CurrentTime,
            vitals: Vitals(
                currentMana: 500,
                maximumMana: 1000),
            spellbook: spellbook);
        var resumed = scenario.Dispatch(
            restoredMana.RaisedEvents.Single());

        Assert.Multiple(() =>
        {
            Assert.That(
                ((CastSpellIntent)restoring.Intent!).SpellName,
                Is.EqualTo(FlowerSpellNames.ManaRestoration));
            Assert.That(
                completed.State.SpellCast?.Status,
                Is.EqualTo(SpellCastStatus.Succeeded));
            Assert.That(waiting.Intent, Is.Null);
            Assert.That(waiting.State.PendingAction, Is.Null);
            Assert.That(
                waiting.State.SpellCooldowns.GetReadyAt(
                    FlowerSpellNames.ManaRestoration,
                    unchangedMana.State.LatestSnapshot!.CaptureCompletedAt),
                Is.Not.Null);
            Assert.That(resumed.Intent, Is.TypeOf<CastSpellIntent>());
            Assert.That(
                ((CastSpellIntent)resumed.Intent!).SpellName,
                Is.EqualTo(queuedSpell.Name));
        });
    }

    [Test]
    public void ShouldRetryManaRestorationAfterObservationWindow()
    {
        var scenario = new MacroScenario();
        var queuedEntry = SpellEntry();
        var queuedSpell = Spell(
            queuedEntry.Name,
            slot: 1,
            manaCost: 500);
        var restoration = Spell(
            FlowerSpellNames.ManaRestoration,
            slot: 2);
        var spellbook = new SpellbookSnapshot(
            [queuedSpell, restoration]);
        scenario.Observe(
            sequence: 1,
            activePanel: ClientPanel.TemuairSpells,
            vitals: Vitals(
                currentMana: 100,
                maximumMana: 1000),
            spellbook: spellbook);
        scenario.Send(new AddSpellQueueEntryCommand(queuedEntry));
        scenario.Send(
            new ConfigureAutomationCommand(
                new AutomationConfiguration(
                    spellsEnabled: true,
                    spellPolicy: TestSpellPolicy,
                    flowerPolicy: new FlowerExecutionPolicy(
                        spell: TestSpellPolicy,
                        restoreManaOnDemand: true))));
        var started = scenario.Start();
        var restoring = scenario.Dispatch(
            started.RaisedEvents.Single());
        var deadline = restoring.ScheduledEvents.Single();
        scenario.AdvanceBy(
            deadline.DueAt.Elapsed - scenario.CurrentTime.Elapsed);
        scenario.Dispatch(deadline.Input);

        scenario.AdvanceBy(TimeSpan.FromSeconds(2));
        var unchangedMana = scenario.Observe(
            sequence: 2,
            activePanel: ClientPanel.TemuairSpells,
            captureStartedAt: scenario.CurrentTime,
            captureCompletedAt: scenario.CurrentTime,
            vitals: Vitals(
                currentMana: 100,
                maximumMana: 1000),
            spellbook: spellbook);
        var retry = scenario.Dispatch(
            unchangedMana.RaisedEvents.Single());

        Assert.Multiple(() =>
        {
            Assert.That(retry.Intent, Is.TypeOf<CastSpellIntent>());
            Assert.That(
                ((CastSpellIntent)retry.Intent!).SpellName,
                Is.EqualTo(FlowerSpellNames.ManaRestoration));
        });
    }

    [Test]
    public void ShouldCancelSpellQueueManaRestorationAfterManaRecovers()
    {
        var scenario = new MacroScenario();
        var queuedEntry = SpellEntry();
        var queuedSpell = Spell(
            queuedEntry.Name,
            slot: 1,
            manaCost: 500);
        var restoration = Spell(
            FlowerSpellNames.ManaRestoration,
            slot: 2);
        scenario.Observe(
            sequence: 1,
            activePanel: ClientPanel.TemuairSpells,
            vitals: Vitals(
                currentMana: 100,
                maximumMana: 1000),
            spellbook: new SpellbookSnapshot(
                [queuedSpell, restoration]));
        scenario.Send(new AddSpellQueueEntryCommand(queuedEntry));
        scenario.Send(
            new ConfigureAutomationCommand(
                new AutomationConfiguration(
                    spellsEnabled: true,
                    spellPolicy: TestSpellPolicy,
                    flowerPolicy: new FlowerExecutionPolicy(
                        spell: TestSpellPolicy,
                        restoreManaOnDemand: true))));
        var started = scenario.Start();
        var restoring = scenario.Dispatch(
            started.RaisedEvents.Single());

        scenario.AdvanceBy(TimeSpan.FromTicks(1));
        var cancelled = scenario.Observe(
            sequence: 2,
            activePanel: ClientPanel.TemuairSpells,
            captureStartedAt: scenario.CurrentTime,
            captureCompletedAt: scenario.CurrentTime,
            vitals: Vitals(
                currentMana: 500,
                maximumMana: 1000),
            spellbook: new SpellbookSnapshot(
                [queuedSpell, restoration]));

        Assert.Multiple(() =>
        {
            Assert.That(
                ((CastSpellIntent)restoring.Intent!).SpellName,
                Is.EqualTo(FlowerSpellNames.ManaRestoration));
            Assert.That(
                restoring.State.SpellCast?.Origin,
                Is.EqualTo(SpellCastOrigin.ManaRestoration));
            Assert.That(
                restoring.State.SpellQueue.Entries,
                Is.EqualTo(new[] { queuedEntry }));
            Assert.That(
                cancelled.Intent,
                Is.TypeOf<CancelSpellIntent>());
            Assert.That(
                cancelled.State.SpellCast?.Status,
                Is.EqualTo(SpellCastStatus.Cancelled));
            Assert.That(cancelled.State.PendingAction, Is.Null);
        });
    }

    [Test]
    public void ShouldWaitForNextFlowerIntervalAfterRosterUpdate()
    {
        var interval = TimeSpan.FromSeconds(5);
        var scenario = new MacroScenario();
        var flowerEntry = new FlowerQueueEntry(
            new FlowerQueueEntryId(1),
            SpellTarget.Self,
            interval);
        var plant = Spell(FlowerSpellNames.Plant, slot: 1);
        var flowerPolicy = new FlowerExecutionPolicy(
            spell: TestSpellPolicy);
        scenario.Observe(
            sequence: 1,
            activePanel: ClientPanel.TemuairSpells,
            vitals: Vitals(),
            spellbook: new SpellbookSnapshot([plant]),
            location: Location());
        scenario.Send(new AddFlowerQueueEntryCommand(flowerEntry));
        scenario.Send(
            new ConfigureAutomationCommand(
                new AutomationConfiguration(
                    floweringEnabled: true,
                    flowerBeforeSpells: true,
                    flowerPolicy: flowerPolicy)));
        var started = scenario.Start();
        var waiting = scenario.Dispatch(
            started.RaisedEvents.Single());

        scenario.AdvanceBy(interval);
        var observed = scenario.Observe(
            sequence: 2,
            activePanel: ClientPanel.TemuairSpells,
            captureStartedAt: scenario.CurrentTime,
            captureCompletedAt: scenario.CurrentTime,
            vitals: Vitals(),
            spellbook: new SpellbookSnapshot([plant]),
            location: Location());
        var cast = scenario.Dispatch(
            observed.RaisedEvents.Single());
        scenario.AdvanceBy(
            cast.ScheduledEvents.Single().DueAt.Elapsed -
            scenario.CurrentTime.Elapsed);
        var completed = scenario.Dispatch(
            cast.ScheduledEvents.Single().Input);

        scenario.AdvanceBy(TimeSpan.FromSeconds(1));
        scenario.Observe(
            sequence: 3,
            activePanel: ClientPanel.TemuairSpells,
            captureStartedAt: scenario.CurrentTime,
            captureCompletedAt: scenario.CurrentTime,
            vitals: Vitals(),
            spellbook: new SpellbookSnapshot([plant]),
            location: Location());
        var rosterUpdated = scenario.ObserveClientRoster(
            sequence: 1,
            clients: []);
        var nextCycle = scenario.Dispatch(
            rosterUpdated.RaisedEvents.Single());

        Assert.Multiple(() =>
        {
            Assert.That(
                waiting.State.Flower?.Status,
                Is.EqualTo(FlowerStatus.WaitingForTarget));
            Assert.That(
                completed.State.SpellCast?.Status,
                Is.EqualTo(SpellCastStatus.Succeeded));
            Assert.That(
                completed.State.Flower?.Status,
                Is.EqualTo(FlowerStatus.Succeeded));
            Assert.That(
                nextCycle.State.SpellCast?.Status,
                Is.EqualTo(SpellCastStatus.Succeeded));
            Assert.That(
                nextCycle.State.Flower?.Status,
                Is.EqualTo(FlowerStatus.WaitingForTarget));
            Assert.That(nextCycle.State.Flower?.Action, Is.Null);
            Assert.That(nextCycle.Intent, Is.Null);
        });
    }

    [Test]
    public void ShouldWaitForAFreshSnapshotAfterAnActionCompletes()
    {
        var scenario = new MacroScenario();
        var entry = SpellEntry();
        var spell = Spell(entry.Name, slot: 1);
        scenario.Observe(
            sequence: 1,
            activePanel: ClientPanel.TemuairSpells,
            vitals: Vitals(),
            spellbook: new SpellbookSnapshot([spell]));
        scenario.Send(new AddSpellQueueEntryCommand(entry));
        scenario.Send(
            new ConfigureAutomationCommand(
                new AutomationConfiguration(
                    spellsEnabled: true,
                    spellPolicy: TestSpellPolicy)));
        var started = scenario.Start();
        var requested = scenario.Dispatch(
            started.RaisedEvents.Single());
        var deadline = requested.ScheduledEvents.Single();
        scenario.AdvanceBy(
            deadline.DueAt.Elapsed - scenario.CurrentTime.Elapsed);
        var completed = scenario.Dispatch(deadline.Input);

        var stale = scenario.Dispatch(
            new AutomationCycleRequested());
        scenario.AdvanceBy(TimeSpan.FromTicks(1));
        var observed = scenario.Observe(
            sequence: 2,
            activePanel: ClientPanel.TemuairSpells,
            captureStartedAt: scenario.CurrentTime,
            captureCompletedAt: scenario.CurrentTime,
            vitals: Vitals(),
            spellbook: new SpellbookSnapshot([spell]));
        var next = scenario.Dispatch(
            observed.RaisedEvents.Single());

        Assert.Multiple(() =>
        {
            Assert.That(
                completed.State.SpellCast?.Status,
                Is.EqualTo(SpellCastStatus.Succeeded));
            Assert.That(stale.State, Is.SameAs(completed.State));
            Assert.That(stale.PublishedView, Is.Null);
            Assert.That(next.Intent, Is.TypeOf<CastSpellIntent>());
        });
    }

    [Test]
    public void ShouldUseSkillsWhileASpellIsCasting()
    {
        var scenario = new MacroScenario();
        var spellEntry = SpellEntry();
        var spell = new SpellSnapshot(
            spellEntry.Name,
            slot: 73,
            currentLevel: 0,
            maximumLevel: 100,
            castLines: 2,
            manaCost: 0,
            cooldown: TimeSpan.Zero);
        var skillEntry = new SkillQueueEntry(
            new SkillQueueEntryId(1),
            "queued skill");
        var skill = new SkillSnapshot(
            skillEntry.Name,
            slot: 73,
            currentLevel: 0,
            maximumLevel: 100,
            manaCost: 0,
            cooldown: TimeSpan.Zero);
        scenario.Observe(
            sequence: 1,
            activePanel: ClientPanel.WorldSpells,
            vitals: Vitals(),
            spellbook: new SpellbookSnapshot([spell]),
            skillbook: new SkillbookSnapshot([skill]));
        scenario.Send(new AddSpellQueueEntryCommand(spellEntry));
        scenario.Send(new AddSkillQueueEntryCommand(skillEntry));
        scenario.Send(
            new ConfigureAutomationCommand(
                new AutomationConfiguration(
                    spellsEnabled: true,
                    skillsEnabled: true,
                    spellPolicy: TestSpellPolicy)));
        var started = scenario.Start();
        var cast = scenario.Dispatch(started.RaisedEvents.Single());
        var castDeadline = cast.ScheduledEvents.Single();

        scenario.AdvanceBy(TimeSpan.FromMilliseconds(1));
        var observed = scenario.Observe(
            sequence: 2,
            activePanel: ClientPanel.WorldSpells,
            vitals: Vitals(),
            spellbook: new SpellbookSnapshot([spell]),
            skillbook: new SkillbookSnapshot([skill]));
        var usedSkill = scenario.Dispatch(
            observed.RaisedEvents.Single());

        scenario.AdvanceBy(
            castDeadline.DueAt.Elapsed -
            scenario.CurrentTime.Elapsed);
        var completedCast = scenario.Dispatch(castDeadline.Input);

        Assert.Multiple(() =>
        {
            Assert.That(cast.Intent, Is.TypeOf<CastSpellIntent>());
            Assert.That(cast.State.PendingAction, Is.Null);
            Assert.That(
                cast.State.SpellCast?.Status,
                Is.EqualTo(SpellCastStatus.Casting));
            Assert.That(
                usedSkill.Intent,
                Is.TypeOf<UseSkillIntent>());
            Assert.That(
                usedSkill.State.PendingAction?.Intent,
                Is.TypeOf<UseSkillIntent>());
            Assert.That(
                usedSkill.State.SpellCast?.Status,
                Is.EqualTo(SpellCastStatus.Casting));
            Assert.That(
                completedCast.State.SpellCast?.Status,
                Is.EqualTo(SpellCastStatus.Succeeded));
            Assert.That(
                completedCast.State.PendingAction?.Intent,
                Is.TypeOf<UseSkillIntent>());
        });
    }

    [TestCase(
        SpellQueueRotation.RoundRobin,
        SpellQueueRotation.Priority,
        false,
        "first spell")]
    [TestCase(
        SpellQueueRotation.Priority,
        SpellQueueRotation.Priority,
        true,
        "second spell")]
    public void ShouldUseLiveQueueEditsForTheNextAutomationCast(
        SpellQueueRotation initialRotation,
        SpellQueueRotation replacementRotation,
        bool reverseOrder,
        string expectedNextSpell)
    {
        var scenario = new MacroScenario();
        var first = new SpellQueueEntry(
            new SpellQueueEntryId(1),
            "first spell",
            target: SpellTarget.Self);
        var second = new SpellQueueEntry(
            new SpellQueueEntryId(2),
            "second spell",
            target: SpellTarget.Self);
        var spellbook = new SpellbookSnapshot(
        [
            Spell(first.Name, slot: 1),
            Spell(second.Name, slot: 2)
        ]);
        var configuration = new AutomationConfiguration(
            spellsEnabled: true,
            spellPolicy: TestSpellPolicy);
        scenario.Observe(
            sequence: 1,
            activePanel: ClientPanel.TemuairSpells,
            vitals: Vitals(),
            spellbook: spellbook);
        scenario.Send(
            new ApplyAutomationSetupCommand(
                new ReplaceQueuesCommand(
                    [first, second],
                    initialRotation,
                    skills: [],
                    flowers: []),
                configuration));
        var started = scenario.Start();
        var firstCast = scenario.Dispatch(
            started.RaisedEvents.Single());
        SpellQueueEntry[] replacement = reverseOrder
            ? [second, first]
            : [first, second];

        var applied = scenario.Send(
            new ApplyAutomationSetupCommand(
                new ReplaceQueuesCommand(
                    replacement,
                    replacementRotation,
                    skills: [],
                    flowers: []),
                configuration));
        scenario.AdvanceBy(
            firstCast.ScheduledEvents.Single().DueAt.Elapsed -
            scenario.CurrentTime.Elapsed);
        scenario.Dispatch(firstCast.ScheduledEvents.Single().Input);
        scenario.AdvanceBy(TimeSpan.FromTicks(1));
        var observed = scenario.Observe(
            sequence: 2,
            activePanel: ClientPanel.TemuairSpells,
            captureStartedAt: scenario.CurrentTime,
            captureCompletedAt: scenario.CurrentTime,
            vitals: Vitals(),
            spellbook: spellbook);
        var nextCast = scenario.Dispatch(
            observed.RaisedEvents.Single());

        Assert.Multiple(() =>
        {
            Assert.That(
                ((CastSpellIntent)firstCast.Intent!).SpellName,
                Is.EqualTo(first.Name));
            Assert.That(
                applied.State.Lifecycle,
                Is.EqualTo(MacroLifecycle.Running));
            Assert.That(
                applied.State.SpellQueue.Rotation,
                Is.EqualTo(replacementRotation));
            Assert.That(
                applied.State.SpellQueue.Entries,
                Is.EqualTo(replacement));
            Assert.That(
                ((CastSpellIntent)nextCast.Intent!).SpellName,
                Is.EqualTo(expectedNextSpell));
        });
    }

    [Test]
    public void ShouldNotPublishRepeatedNoOpAutomationState()
    {
        var scenario = new MacroScenario();
        scenario.Observe(sequence: 1);
        scenario.Send(
            new ConfigureAutomationCommand(
                new AutomationConfiguration(
                    spellsEnabled: true)));
        var started = scenario.Start();
        var first = scenario.Dispatch(
            started.RaisedEvents.Single());

        var repeated = scenario.Dispatch(
            new AutomationCycleRequested());
        scenario.AdvanceBy(TimeSpan.FromTicks(1));
        var observed = scenario.Observe(
            sequence: 2,
            captureStartedAt: scenario.CurrentTime,
            captureCompletedAt: scenario.CurrentTime);
        var afterSnapshot = scenario.Dispatch(
            observed.RaisedEvents.Single());

        Assert.Multiple(() =>
        {
            Assert.That(
                first.State.SpellCast?.Status,
                Is.EqualTo(SpellCastStatus.QueueEmpty));
            Assert.That(repeated.State, Is.SameAs(first.State));
            Assert.That(repeated.PublishedView, Is.Null);
            Assert.That(repeated.Intent, Is.Null);
            Assert.That(afterSnapshot.State, Is.SameAs(observed.State));
            Assert.That(afterSnapshot.PublishedView, Is.Null);
        });
    }

    [Test]
    public void ShouldValidateAutomationConfigurationCommands()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                AutomationConfiguration.Disabled.IsEnabled,
                Is.False);
            Assert.That(
                AutomationConfiguration.Disabled.PanelPreservation.Enabled,
                Is.False);
            Assert.That(
                () => _ = new ConfigureAutomationCommand(null!),
                Throws.TypeOf<ArgumentNullException>());
        });
    }

    private static SpellQueueEntry SpellEntry() =>
        new(
            new SpellQueueEntryId(1),
            "queued spell",
            targetLevel: null,
            SpellTarget.Self);

    private static SpellSnapshot Spell(
        string name,
        int slot,
        int manaCost = 0) =>
        new(
            name,
            slot,
            currentLevel: 0,
            maximumLevel: 100,
            castLines: 1,
            manaCost,
            cooldown: TimeSpan.Zero);

    private static VitalsSnapshot Vitals(
        int currentMana = 100,
        int maximumMana = 100) =>
        new(
            currentHealth: 100,
            maximumHealth: 100,
            currentMana,
            maximumMana);

    private static MapLocationSnapshot Location() =>
        new(
            mapNumber: 1,
            mapName: "test map",
            x: 50,
            y: 50);

    private static ClientSnapshot Snapshot(
        long sequence,
        MacroTimestamp capturedAt,
        ClientPanel panel,
        IEnumerable<SpellSnapshot> spells) =>
        new(
            new SnapshotSequence(sequence),
            capturedAt,
            capturedAt,
            new ClientIdentity("session-client"),
            SnapshotQuality.Complete,
            ClientPresence.InWorld,
            panel,
            vitals: Vitals(),
            spellbook: new SpellbookSnapshot(spells));
}
