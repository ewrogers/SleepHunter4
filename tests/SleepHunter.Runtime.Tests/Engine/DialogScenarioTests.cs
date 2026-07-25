using SleepHunter.Runtime.Actions;
using SleepHunter.Runtime.Automation.Dialogs;
using SleepHunter.Runtime.Automation.Equipment;
using SleepHunter.Runtime.Automation.Panels;
using SleepHunter.Runtime.Automation.Skills;
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

public sealed class DialogScenarioTests
{
    private static readonly DialogPolicy TestDialogPolicy = new(
        TimeSpan.FromMilliseconds(100),
        TimeSpan.FromMilliseconds(25));

    private static readonly SkillExecutionPolicy TestPolicy = new(
        SkillUsePolicy.Default,
        PanelTransitionPolicy.Default,
        DisarmPolicy.Default,
        TimeSpan.FromMilliseconds(10),
        TestDialogPolicy);

    [Test]
    public void ShouldScheduleAndCloseDialogAfterOpeningSkill()
    {
        var scenario = CreateRunningScenario();

        var skillRequested = scenario.Send(
            new UseNextSkillCommand(TestPolicy));
        var skillDeadline = skillRequested.ScheduledEvents.Single(
            scheduledEvent =>
                scheduledEvent.Input is ClientActionDeadlineElapsed);
        var dialogDue = skillRequested.ScheduledEvents.Single(
            scheduledEvent => scheduledEvent.Input is DialogCloseDue);

        scenario.AdvanceBy(TestPolicy.ActionDuration);
        scenario.Dispatch(skillDeadline.Input);
        AdvanceTo(scenario, dialogDue.DueAt);
        var closeRequested = scenario.Dispatch(dialogDue.Input);

        scenario.AdvanceBy(TestDialogPolicy.ActionDuration);
        var closed = scenario.Dispatch(
            closeRequested.ScheduledEvents.Single().Input);

        Assert.Multiple(() =>
        {
            Assert.That(
                skillRequested.State.Dialog?.Status,
                Is.EqualTo(DialogStatus.Scheduled));
            Assert.That(
                skillRequested.State.Dialog?.DueAt,
                Is.EqualTo(dialogDue.DueAt));
            Assert.That(
                closeRequested.Intent,
                Is.TypeOf<CancelDialogIntent>());
            Assert.That(
                closeRequested.State.Dialog?.Status,
                Is.EqualTo(DialogStatus.Closing));
            Assert.That(
                closed.State.Dialog?.Status,
                Is.EqualTo(DialogStatus.Closed));
            Assert.That(closed.State.PendingAction, Is.Null);
        });
    }

    [Test]
    public void ShouldPauseWhenDialogCloseIssuanceFails()
    {
        var scenario = CreateRunningScenario(issueActions: false);
        var skillRequested = scenario.Send(
            new UseNextSkillCommand(TestPolicy));
        var skillIntent = (UseSkillIntent)skillRequested.Intent!;
        scenario.Dispatch(
            new ClientActionIssueObserved(
                new ClientActionIssue(
                    skillIntent.ActionId,
                    ClientActionIssueStatus.Issued)));
        scenario.AdvanceBy(TestPolicy.ActionDuration);
        scenario.Dispatch(
            skillRequested.ScheduledEvents.Single(
                scheduledEvent =>
                    scheduledEvent.Input is ClientActionDeadlineElapsed).Input);
        var dialogDue = skillRequested.ScheduledEvents.Single(
            scheduledEvent => scheduledEvent.Input is DialogCloseDue);
        AdvanceTo(scenario, dialogDue.DueAt);
        var closeRequested = scenario.Dispatch(dialogDue.Input);

        var failed = scenario.Dispatch(
            new ClientActionIssueObserved(
                new ClientActionIssue(
                    ((CancelDialogIntent)closeRequested.Intent!).ActionId,
                    ClientActionIssueStatus.PartiallyIssued)));

        Assert.Multiple(() =>
        {
            Assert.That(failed.State.Lifecycle, Is.EqualTo(MacroLifecycle.Paused));
            Assert.That(failed.State.PendingAction, Is.Null);
            Assert.That(
                failed.State.Dialog?.Status,
                Is.EqualTo(DialogStatus.IssueFailed));
        });
    }

    [Test]
    public void ShouldIgnoreCloseEventSupersededByNewerDialog()
    {
        var scenario = CreateRunningScenario();
        var first = scenario.Send(new UseNextSkillCommand(TestPolicy));
        var firstSkillDeadline = first.ScheduledEvents.Single(
            scheduledEvent =>
                scheduledEvent.Input is ClientActionDeadlineElapsed);
        var firstDialogDue = first.ScheduledEvents.Single(
            scheduledEvent => scheduledEvent.Input is DialogCloseDue);

        scenario.AdvanceBy(TestPolicy.ActionDuration);
        scenario.Dispatch(firstSkillDeadline.Input);
        scenario.AdvanceBy(TimeSpan.FromTicks(1));
        scenario.Observe(
            sequence: 2,
            activePanel: ClientPanel.TemuairSkills,
            vitals: Vitals(),
            skillbook: Skillbook());
        var second = scenario.Send(new UseNextSkillCommand(TestPolicy));
        var secondDialogDue = second.ScheduledEvents.Single(
            scheduledEvent => scheduledEvent.Input is DialogCloseDue);

        AdvanceTo(scenario, firstDialogDue.DueAt);
        var stale = scenario.Dispatch(firstDialogDue.Input);

        Assert.Multiple(() =>
        {
            Assert.That(secondDialogDue.DueAt, Is.GreaterThan(firstDialogDue.DueAt));
            Assert.That(stale.Intent, Is.Null);
            Assert.That(stale.State, Is.SameAs(second.State));
            Assert.That(
                stale.State.Dialog?.DueAt,
                Is.EqualTo(secondDialogDue.DueAt));
        });
    }

    [Test]
    public void ShouldDeferDialogCloseUntilActiveClientActionFinishes()
    {
        var scenario = CreateRunningScenario();
        var skillRequested = scenario.Send(new UseNextSkillCommand(TestPolicy));
        var skillDeadline = skillRequested.ScheduledEvents.Single(
            scheduledEvent =>
                scheduledEvent.Input is ClientActionDeadlineElapsed);
        var dialogDue = skillRequested.ScheduledEvents.Single(
            scheduledEvent => scheduledEvent.Input is DialogCloseDue);

        scenario.AdvanceBy(TestPolicy.ActionDuration);
        scenario.Dispatch(skillDeadline.Input);
        scenario.AdvanceBy(TimeSpan.FromMilliseconds(80));
        var panelRequested = scenario.Send(
            new RequestPanelTransitionCommand(
                ClientPanel.Inventory,
                new PanelTransitionPolicy(
                    TimeSpan.FromMilliseconds(50),
                    maximumAttempts: 1)));

        AdvanceTo(scenario, dialogDue.DueAt);
        var deferred = scenario.Dispatch(dialogDue.Input);
        var rescheduled = deferred.ScheduledEvents.Single();
        AdvanceTo(scenario, panelRequested.State.PendingAction!.Deadline);
        scenario.Dispatch(panelRequested.ScheduledEvents.Single().Input);
        AdvanceTo(scenario, rescheduled.DueAt);
        var closeRequested = scenario.Dispatch(rescheduled.Input);

        Assert.Multiple(() =>
        {
            Assert.That(deferred.Intent, Is.Null);
            Assert.That(
                deferred.State.PendingAction?.Intent,
                Is.TypeOf<SwitchPanelIntent>());
            Assert.That(
                deferred.State.Dialog?.Status,
                Is.EqualTo(DialogStatus.Scheduled));
            Assert.That(
                rescheduled.DueAt,
                Is.GreaterThan(panelRequested.State.PendingAction.Deadline));
            Assert.That(
                closeRequested.Intent,
                Is.TypeOf<CancelDialogIntent>());
        });
    }

    [Test]
    public void ShouldCancelScheduledAndClosingDialogForLifecycleInterruptions()
    {
        var scheduledScenario = CreateRunningScenario();
        var scheduled = scheduledScenario.Send(
            new UseNextSkillCommand(TestPolicy));
        var paused = scheduledScenario.Pause();
        var scheduledClose = scheduled.ScheduledEvents.Single(
            scheduledEvent => scheduledEvent.Input is DialogCloseDue);
        AdvanceTo(scheduledScenario, scheduledClose.DueAt);
        var stale = scheduledScenario.Dispatch(scheduledClose.Input);

        var closingScenario = CreateRunningScenario();
        var opening = closingScenario.Send(
            new UseNextSkillCommand(TestPolicy));
        var skillDeadline = opening.ScheduledEvents.Single(
            scheduledEvent =>
                scheduledEvent.Input is ClientActionDeadlineElapsed);
        var dialogDue = opening.ScheduledEvents.Single(
            scheduledEvent => scheduledEvent.Input is DialogCloseDue);
        closingScenario.AdvanceBy(TestPolicy.ActionDuration);
        closingScenario.Dispatch(skillDeadline.Input);
        AdvanceTo(closingScenario, dialogDue.DueAt);
        closingScenario.Dispatch(dialogDue.Input);
        var stopped = closingScenario.Stop();

        var logoutScenario = CreateRunningScenario();
        logoutScenario.Send(new UseNextSkillCommand(TestPolicy));
        var loggedOut = logoutScenario.Observe(
            sequence: 2,
            presence: ClientPresence.LoggedOut,
            activePanel: ClientPanel.TemuairSkills);

        Assert.Multiple(() =>
        {
            Assert.That(
                paused.State.Dialog?.Status,
                Is.EqualTo(DialogStatus.Cancelled));
            Assert.That(stale.State, Is.SameAs(paused.State));
            Assert.That(
                stopped.State.Dialog?.Status,
                Is.EqualTo(DialogStatus.Cancelled));
            Assert.That(stopped.State.PendingAction, Is.Null);
            Assert.That(
                loggedOut.State.Dialog?.Status,
                Is.EqualTo(DialogStatus.Cancelled));
            Assert.That(
                loggedOut.State.StopReason,
                Is.EqualTo(MacroStopReason.ClientLoggedOut));
        });
    }

    [Test]
    public async Task ShouldDispatchDialogCloseThroughSessionScheduler()
    {
        var timeProvider = new ManualTimeProvider();
        await using var session = new MacroSession(
            new MacroEngine(),
            new MacroClock(timeProvider));
        var entry = Entry();
        var snapshot = Snapshot(
            sequence: 1,
            MacroTimestamp.Zero);

        session.PublishSnapshot(snapshot);
        await session.Views.ReadUntilAsync(view => view.Revision == 1);
        await session.SendCommandAsync(new AddSkillQueueEntryCommand(entry));
        await session.Views.ReadUntilAsync(view => view.Revision == 2);
        await session.SendCommandAsync(new StartMacroCommand());
        await session.Views.ReadUntilAsync(view => view.Revision == 3);
        await session.SendCommandAsync(new UseNextSkillCommand(TestPolicy));

        var skillIntent = await session.Intents.ReadUntilAsync(
            intent => intent is UseSkillIntent);
        await session.ReportActionIssueAsync(
            new ClientActionIssue(
                ((UseSkillIntent)skillIntent).ActionId,
                ClientActionIssueStatus.Issued));
        timeProvider.Advance(TestPolicy.ActionDuration);
        await session.Views.ReadUntilAsync(
            view => view.SkillUse?.Status == SkillUseStatus.Succeeded);
        timeProvider.Advance(
            TestDialogPolicy.CloseDelay - TestPolicy.ActionDuration);
        var closeIntent = await session.Intents.ReadUntilAsync(
            intent => intent is CancelDialogIntent);
        await session.ReportActionIssueAsync(
            new ClientActionIssue(
                ((CancelDialogIntent)closeIntent).ActionId,
                ClientActionIssueStatus.Issued));
        timeProvider.Advance(TestDialogPolicy.ActionDuration);
        var closed = await session.Views.ReadUntilAsync(
            view => view.Dialog?.Status == DialogStatus.Closed);

        Assert.Multiple(() =>
        {
            Assert.That(skillIntent, Is.TypeOf<UseSkillIntent>());
            Assert.That(closeIntent, Is.TypeOf<CancelDialogIntent>());
            Assert.That(
                ((CancelDialogIntent)closeIntent).ActionId.Value,
                Is.EqualTo(2));
            Assert.That(closed.PendingActionId, Is.Null);
        });
    }

    private static MacroScenario CreateRunningScenario(
        bool issueActions = true)
    {
        var scenario = new MacroScenario(issueActions: issueActions);
        scenario.Send(new AddSkillQueueEntryCommand(Entry()));
        scenario.Observe(
            sequence: 1,
            activePanel: ClientPanel.TemuairSkills,
            vitals: Vitals(),
            skillbook: Skillbook());
        scenario.Start();
        return scenario;
    }

    private static SkillQueueEntry Entry() =>
        new(new SkillQueueEntryId(1), "dialog skill");

    private static SkillbookSnapshot Skillbook() =>
        new(
        [
            new SkillSnapshot(
                "dialog skill",
                slot: 1,
                currentLevel: 0,
                maximumLevel: 100,
                manaCost: 0,
                cooldown: TimeSpan.Zero,
                opensDialog: true)
        ]);

    private static VitalsSnapshot Vitals() =>
        new(
            currentHealth: 100,
            maximumHealth: 100,
            currentMana: 100,
            maximumMana: 100);

    private static ClientSnapshot Snapshot(
        long sequence,
        MacroTimestamp capturedAt) =>
        new(
            new SnapshotSequence(sequence),
            capturedAt,
            capturedAt,
            new ClientIdentity("dialog-session-client"),
            SnapshotQuality.Complete,
            ClientPresence.InWorld,
            ClientPanel.TemuairSkills,
            character: null,
            inventory: null,
            equipment: null,
            Vitals(),
            spellbook: null,
            Skillbook());

    private static void AdvanceTo(
        MacroScenario scenario,
        MacroTimestamp timestamp) =>
        scenario.AdvanceBy(timestamp.Elapsed - scenario.CurrentTime.Elapsed);
}
