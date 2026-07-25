using SleepHunter.Runtime.Actions;
using SleepHunter.Runtime.Automation;
using SleepHunter.Runtime.Automation.Panels;
using SleepHunter.Runtime.Automation.Skills;
using SleepHunter.Runtime.Automation.Spells;
using SleepHunter.Runtime.Commands;
using SleepHunter.Runtime.Engine;
using SleepHunter.Runtime.Events;
using SleepHunter.Runtime.Intents;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Tests.Scenarios;

namespace SleepHunter.Runtime.Tests.Engine;

public sealed class PanelPreservationScenarioTests
{
    private static readonly PanelTransitionPolicy TestPanelPolicy = new(
        TimeSpan.FromMilliseconds(25),
        maximumAttempts: 2);

    private static readonly PanelPreservationPolicy TestPreservation = new(
        enabled: true,
        TestPanelPolicy);

    private static readonly SpellExecutionPolicy TestSpellPolicy = new(
        new SpellCastPolicy(
            requireMana: true,
            new SpellCastTimingPolicy(
                zeroLineDuration: TimeSpan.Zero,
                singleLineDuration: TimeSpan.FromMilliseconds(10),
                multiLineDurationPerLine: TimeSpan.FromMilliseconds(10),
                completionPadding: TimeSpan.FromMilliseconds(1))),
        TestPanelPolicy,
        allowStaffSwitching: false);

    private static readonly SkillExecutionPolicy TestSkillPolicy = new(
        panelTransition: TestPanelPolicy,
        actionDuration: TimeSpan.FromMilliseconds(10));

    [Test]
    public void ShouldRestorePanelAfterAutomaticSpellCompletes()
    {
        var scenario = CreateSpellScenario(
            ClientPanel.Stats,
            TestPreservation);
        var panelRequested = StartAutomation(scenario);

        var castRequested = ConfirmPanel(
            scenario,
            sequence: 2,
            ClientPanel.TemuairSpells,
            Spellbook());
        CompletePendingAction(scenario, castRequested);
        var restoreRequested = ObserveAndRunCycle(
            scenario,
            sequence: 3,
            ClientPanel.TemuairSpells,
            Spellbook());
        var restored = ConfirmPanel(
            scenario,
            sequence: 4,
            ClientPanel.Stats,
            Spellbook());

        Assert.Multiple(() =>
        {
            Assert.That(
                panelRequested.Intent,
                Is.TypeOf<SwitchPanelIntent>());
            Assert.That(
                panelRequested.State.PanelPreservation?.OriginalPanel,
                Is.EqualTo(ClientPanel.Stats));
            Assert.That(castRequested.Intent, Is.TypeOf<CastSpellIntent>());
            Assert.That(
                restoreRequested.Intent,
                Is.EqualTo(
                    new SwitchPanelIntent(
                        ((SwitchPanelIntent)restoreRequested.Intent!).ActionId,
                        ClientPanel.Stats)));
            Assert.That(
                restoreRequested.State.PanelPreservation?.Status,
                Is.EqualTo(PanelPreservationStatus.Restoring));
            Assert.That(
                restored.State.PanelPreservation?.Status,
                Is.EqualTo(PanelPreservationStatus.Succeeded));
            Assert.That(restored.State.PendingAction, Is.Null);
            Assert.That(restored.RaisedEvents, Is.Empty);
        });
    }

    [Test]
    public void ShouldRestorePanelAfterAutomaticSkillCompletes()
    {
        var scenario = CreateSkillScenario(ClientPanel.Chat);
        StartAutomation(scenario);

        var skillRequested = ConfirmPanel(
            scenario,
            sequence: 2,
            ClientPanel.TemuairSkills,
            skillbook: Skillbook());
        CompletePendingAction(scenario, skillRequested);
        var restoreRequested = ObserveAndRunCycle(
            scenario,
            sequence: 3,
            ClientPanel.TemuairSkills,
            skillbook: Skillbook());
        var restored = ConfirmPanel(
            scenario,
            sequence: 4,
            ClientPanel.Chat,
            skillbook: Skillbook());

        Assert.Multiple(() =>
        {
            Assert.That(
                skillRequested.Intent,
                Is.TypeOf<UseSkillIntent>());
            Assert.That(
                ((SwitchPanelIntent)restoreRequested.Intent!).TargetPanel,
                Is.EqualTo(ClientPanel.Chat));
            Assert.That(
                restored.State.PanelPreservation?.Status,
                Is.EqualTo(PanelPreservationStatus.Succeeded));
        });
    }

    [Test]
    public void ShouldCompleteWithoutRestoreIntentWhenPanelIsUnchanged()
    {
        var scenario = CreateSpellScenario(
            ClientPanel.TemuairSpells,
            TestPreservation);
        var castRequested = StartAutomation(scenario);
        CompletePendingAction(scenario, castRequested);

        var completed = ObserveAndRunCycle(
            scenario,
            sequence: 2,
            ClientPanel.TemuairSpells,
            Spellbook());

        Assert.Multiple(() =>
        {
            Assert.That(completed.Intent, Is.Null);
            Assert.That(completed.State.PendingAction, Is.Null);
            Assert.That(
                completed.State.PanelPreservation?.Status,
                Is.EqualTo(PanelPreservationStatus.Succeeded));
        });
    }

    [Test]
    public void ShouldNotTrackPanelWhenNoAutomaticActionStarts()
    {
        var scenario = new MacroScenario();
        scenario.Observe(
            sequence: 1,
            activePanel: ClientPanel.Stats);
        scenario.Send(
            new ConfigureAutomationCommand(
                new AutomationConfiguration(
                    spellsEnabled: true,
                    panelPreservation: TestPreservation)));

        var cycle = StartAutomation(scenario);

        Assert.Multiple(() =>
        {
            Assert.That(cycle.Intent, Is.Null);
            Assert.That(cycle.State.PendingAction, Is.Null);
            Assert.That(cycle.State.PanelPreservation, Is.Null);
        });
    }

    [Test]
    public void ShouldNotTrackPanelWhenPreservationIsDisabled()
    {
        var scenario = CreateSpellScenario(
            ClientPanel.Stats,
            PanelPreservationPolicy.Disabled);

        var cycle = StartAutomation(scenario);

        Assert.Multiple(() =>
        {
            Assert.That(cycle.Intent, Is.TypeOf<SwitchPanelIntent>());
            Assert.That(cycle.State.PanelPreservation, Is.Null);
        });
    }

    [Test]
    public void ShouldExposeBoundedRestoreTimeout()
    {
        var scenario = CreateSpellScenario(
            ClientPanel.Stats,
            TestPreservation);
        StartAutomation(scenario);
        var castRequested = ConfirmPanel(
            scenario,
            sequence: 2,
            ClientPanel.TemuairSpells,
            Spellbook());
        CompletePendingAction(scenario, castRequested);
        var firstRestore = ObserveAndRunCycle(
            scenario,
            sequence: 3,
            ClientPanel.TemuairSpells,
            Spellbook());

        var retry = CompletePendingAction(scenario, firstRestore);
        var timedOut = CompletePendingAction(scenario, retry);

        Assert.Multiple(() =>
        {
            Assert.That(retry.Intent, Is.TypeOf<SwitchPanelIntent>());
            Assert.That(
                timedOut.State.PanelPreservation?.Status,
                Is.EqualTo(PanelPreservationStatus.TimedOut));
            Assert.That(
                timedOut.State.PanelTransition?.Status,
                Is.EqualTo(PanelTransitionStatus.TimedOut));
            Assert.That(timedOut.State.PendingAction, Is.Null);
            Assert.That(
                timedOut.State.Lifecycle,
                Is.EqualTo(MacroLifecycle.Running));
        });
    }

    [Test]
    public void ShouldCancelTrackedPanelWhenStopped()
    {
        var scenario = CreateSpellScenario(
            ClientPanel.Stats,
            TestPreservation);
        var requested = StartAutomation(scenario);

        var stopped = scenario.Stop();

        Assert.Multiple(() =>
        {
            Assert.That(requested.State.PendingAction, Is.Not.Null);
            Assert.That(stopped.State.PendingAction, Is.Null);
            Assert.That(
                stopped.State.PanelPreservation?.Status,
                Is.EqualTo(PanelPreservationStatus.Cancelled));
            Assert.That(
                stopped.State.PanelTransition?.Status,
                Is.EqualTo(PanelTransitionStatus.Cancelled));
        });
    }

    [Test]
    public void ShouldCancelTrackingWhenPreservationIsDisabled()
    {
        var scenario = CreateSpellScenario(
            ClientPanel.Stats,
            TestPreservation);
        StartAutomation(scenario);

        var configured = scenario.Send(
            new ConfigureAutomationCommand(
                new AutomationConfiguration(
                    spellsEnabled: true,
                    spellPolicy: TestSpellPolicy,
                    panelPreservation:
                        PanelPreservationPolicy.Disabled)));

        Assert.Multiple(() =>
        {
            Assert.That(
                configured.State.PanelPreservation?.Status,
                Is.EqualTo(PanelPreservationStatus.Cancelled));
            Assert.That(configured.State.PendingAction, Is.Not.Null);
            Assert.That(
                configured.State.Automation.PanelPreservation.Enabled,
                Is.False);
        });
    }

    [Test]
    public void ShouldPauseWhenRestoreIntentCannotBeIssued()
    {
        var scenario = CreateSpellScenario(
            ClientPanel.Stats,
            TestPreservation,
            issueActions: false);
        var panelRequested = StartAutomation(scenario);
        ReportIssued(scenario, panelRequested);
        var castRequested = ConfirmPanel(
            scenario,
            sequence: 2,
            ClientPanel.TemuairSpells,
            Spellbook());
        ReportIssued(scenario, castRequested);
        CompletePendingAction(scenario, castRequested);
        var restoreRequested = ObserveAndRunCycle(
            scenario,
            sequence: 3,
            ClientPanel.TemuairSpells,
            Spellbook());
        var restoreIntent = (SwitchPanelIntent)restoreRequested.Intent!;

        var failed = scenario.Dispatch(
            new ClientActionIssueObserved(
                new ClientActionIssue(
                    restoreIntent.ActionId,
                    ClientActionIssueStatus.Failed)));

        Assert.Multiple(() =>
        {
            Assert.That(
                failed.State.Lifecycle,
                Is.EqualTo(MacroLifecycle.Paused));
            Assert.That(failed.State.PendingAction, Is.Null);
            Assert.That(
                failed.State.PanelPreservation?.Status,
                Is.EqualTo(PanelPreservationStatus.IssueFailed));
            Assert.That(
                failed.State.PanelTransition?.Status,
                Is.EqualTo(PanelTransitionStatus.IssueFailed));
        });
    }

    private static MacroScenario CreateSpellScenario(
        ClientPanel activePanel,
        PanelPreservationPolicy preservation,
        bool issueActions = true)
    {
        var scenario = new MacroScenario(issueActions: issueActions);
        scenario.Observe(
            sequence: 1,
            activePanel: activePanel,
            vitals: Vitals(),
            spellbook: Spellbook());
        scenario.Send(new AddSpellQueueEntryCommand(SpellEntry()));
        scenario.Send(
            new ConfigureAutomationCommand(
                new AutomationConfiguration(
                    spellsEnabled: true,
                    spellPolicy: TestSpellPolicy,
                    panelPreservation: preservation)));
        return scenario;
    }

    private static void ReportIssued(
        MacroScenario scenario,
        MacroDecision requested)
    {
        var intent = (ClientActionIntent)requested.Intent!;
        scenario.Dispatch(
            new ClientActionIssueObserved(
                new ClientActionIssue(
                    intent.ActionId,
                    ClientActionIssueStatus.Issued)));
    }

    private static MacroScenario CreateSkillScenario(ClientPanel activePanel)
    {
        var scenario = new MacroScenario();
        scenario.Observe(
            sequence: 1,
            activePanel: activePanel,
            vitals: Vitals(),
            skillbook: Skillbook());
        scenario.Send(
            new AddSkillQueueEntryCommand(
                new SkillQueueEntry(
                    new SkillQueueEntryId(1),
                    "queued skill")));
        scenario.Send(
            new ConfigureAutomationCommand(
                new AutomationConfiguration(
                    skillsEnabled: true,
                    skillPolicy: TestSkillPolicy,
                    panelPreservation: TestPreservation)));
        return scenario;
    }

    private static MacroDecision StartAutomation(MacroScenario scenario)
    {
        var started = scenario.Start();
        return scenario.Dispatch(started.RaisedEvents.Single());
    }

    private static MacroDecision CompletePendingAction(
        MacroScenario scenario,
        MacroDecision requested)
    {
        var actionId =
            ((ClientActionIntent)requested.Intent!).ActionId;
        var deadline = requested.ScheduledEvents.Single(
            scheduledEvent =>
                scheduledEvent.Input is ClientActionDeadlineElapsed elapsed &&
                elapsed.ActionId == actionId);
        scenario.AdvanceBy(
            deadline.DueAt.Elapsed - scenario.CurrentTime.Elapsed);
        return scenario.Dispatch(deadline.Input);
    }

    private static MacroDecision ConfirmPanel(
        MacroScenario scenario,
        long sequence,
        ClientPanel activePanel,
        SpellbookSnapshot? spellbook = null,
        SkillbookSnapshot? skillbook = null)
    {
        scenario.AdvanceBy(TimeSpan.FromTicks(1));
        return scenario.Observe(
            sequence,
            activePanel: activePanel,
            vitals: Vitals(),
            spellbook: spellbook,
            skillbook: skillbook);
    }

    private static MacroDecision ObserveAndRunCycle(
        MacroScenario scenario,
        long sequence,
        ClientPanel activePanel,
        SpellbookSnapshot? spellbook = null,
        SkillbookSnapshot? skillbook = null)
    {
        var observed = ConfirmPanel(
            scenario,
            sequence,
            activePanel,
            spellbook,
            skillbook);
        return scenario.Dispatch(observed.RaisedEvents.Single());
    }

    private static SpellQueueEntry SpellEntry() =>
        new(
            new SpellQueueEntryId(1),
            "queued spell",
            targetLevel: null,
            SpellTarget.Self);

    private static SpellbookSnapshot Spellbook() =>
        new(
            [
                new SpellSnapshot(
                    "queued spell",
                    slot: 1,
                    currentLevel: 0,
                    maximumLevel: 100,
                    castLines: 1,
                    manaCost: 0,
                    cooldown: TimeSpan.FromMinutes(1))
            ]);

    private static SkillbookSnapshot Skillbook() =>
        new(
            [
                new SkillSnapshot(
                    "queued skill",
                    slot: 1,
                    currentLevel: 0,
                    maximumLevel: 100,
                    manaCost: 0,
                    cooldown: TimeSpan.FromMinutes(1))
            ]);

    private static VitalsSnapshot Vitals() =>
        new(
            currentHealth: 100,
            maximumHealth: 100,
            currentMana: 100,
            maximumMana: 100);
}
