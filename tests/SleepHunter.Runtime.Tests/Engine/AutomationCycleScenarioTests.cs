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

    private static SpellSnapshot Spell(string name, int slot) =>
        new(
            name,
            slot,
            currentLevel: 0,
            maximumLevel: 100,
            castLines: 1,
            manaCost: 0,
            cooldown: TimeSpan.Zero);

    private static VitalsSnapshot Vitals() =>
        new(
            currentHealth: 100,
            maximumHealth: 100,
            currentMana: 100,
            maximumMana: 100);

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
