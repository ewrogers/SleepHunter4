using SleepHunter.Runtime.Automation.Panels;
using SleepHunter.Runtime.Commands;
using SleepHunter.Runtime.Engine;
using SleepHunter.Runtime.Events;
using SleepHunter.Runtime.Intents;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Tests.Scenarios;

namespace SleepHunter.Runtime.Tests.Engine;

public sealed class PanelTransitionScenarioTests
{
    private static readonly PanelTransitionPolicy TestPolicy = new(
        TimeSpan.FromMilliseconds(100),
        maximumAttempts: 2);

    [Test]
    public void ShouldIssueBoundedPanelTransitionIntent()
    {
        var scenario = CreateRunningScenario(ClientPanel.Inventory);

        var decision = scenario.Send(
            new RequestPanelTransitionCommand(
                ClientPanel.TemuairSpells,
                TestPolicy));

        var intent = decision.Intent as SwitchPanelIntent;
        var deadline = decision.ScheduledEvents.Single();

        Assert.Multiple(() =>
        {
            Assert.That(intent, Is.Not.Null);
            Assert.That(intent!.ActionId.Value, Is.EqualTo(1));
            Assert.That(
                intent.TargetPanel,
                Is.EqualTo(ClientPanel.TemuairSpells));
            Assert.That(decision.State.PendingAction?.Attempt, Is.EqualTo(1));
            Assert.That(
                decision.State.PendingAction?.MaximumAttempts,
                Is.EqualTo(2));
            Assert.That(
                decision.State.PendingAction?.BaselineSnapshotSequence?.Value,
                Is.EqualTo(1));
            Assert.That(
                decision.State.PendingAction?.Deadline.Elapsed,
                Is.EqualTo(TimeSpan.FromMilliseconds(100)));
            Assert.That(
                deadline.Input,
                Is.EqualTo(new ClientActionDeadlineElapsed(intent.ActionId)));
            Assert.That(
                decision.PublishedView?.PanelTransition?.Status,
                Is.EqualTo(PanelTransitionStatus.Pending));
        });
    }

    [Test]
    public void ShouldSucceedWithoutIntentWhenEquivalentPanelIsAlreadyActive()
    {
        var scenario = CreateRunningScenario(ClientPanel.WorldSkills);

        var decision = scenario.Send(
            new RequestPanelTransitionCommand(
                ClientPanel.WorldSpells,
                TestPolicy));

        Assert.Multiple(() =>
        {
            Assert.That(decision.Intent, Is.Null);
            Assert.That(decision.ScheduledEvents, Is.Empty);
            Assert.That(decision.State.PendingAction, Is.Null);
            Assert.That(
                decision.State.PanelTransition?.Status,
                Is.EqualTo(PanelTransitionStatus.Succeeded));
            Assert.That(decision.State.PanelTransition?.Attempt, Is.Zero);
        });
    }

    [Test]
    public void ShouldRequireSnapshotCapturedAfterIntentForConfirmation()
    {
        var scenario = CreateRunningScenario(ClientPanel.Inventory);
        scenario.Send(
            new RequestPanelTransitionCommand(
                ClientPanel.TemuairSpells,
                TestPolicy));

        var staleCapture = scenario.Observe(
            sequence: 2,
            activePanel: ClientPanel.TemuairSpells);
        scenario.AdvanceBy(TimeSpan.FromTicks(1));
        var confirmed = scenario.Observe(
            sequence: 3,
            activePanel: ClientPanel.TemuairSpells);

        Assert.Multiple(() =>
        {
            Assert.That(staleCapture.State.PendingAction, Is.Not.Null);
            Assert.That(
                staleCapture.State.PanelTransition?.Status,
                Is.EqualTo(PanelTransitionStatus.Pending));
            Assert.That(confirmed.State.PendingAction, Is.Null);
            Assert.That(
                confirmed.State.PanelTransition?.Status,
                Is.EqualTo(PanelTransitionStatus.Succeeded));
            Assert.That(confirmed.State.PanelTransition?.Attempt, Is.EqualTo(1));
        });
    }

    [Test]
    public void ShouldRetryThenExposeStableTimeout()
    {
        var scenario = CreateRunningScenario(ClientPanel.Inventory);
        var firstAttempt = scenario.Send(
            new RequestPanelTransitionCommand(
                ClientPanel.TemuairSpells,
                TestPolicy));

        scenario.AdvanceBy(TestPolicy.AttemptTimeout);
        var retry = scenario.Dispatch(firstAttempt.ScheduledEvents.Single().Input);
        var stateAfterRetry = retry.State;
        var staleDeadline = scenario.Dispatch(
            firstAttempt.ScheduledEvents.Single().Input);
        scenario.AdvanceBy(TestPolicy.AttemptTimeout);
        var timedOut = scenario.Dispatch(retry.ScheduledEvents.Single().Input);

        Assert.Multiple(() =>
        {
            Assert.That(
                ((SwitchPanelIntent)retry.Intent!).ActionId.Value,
                Is.EqualTo(2));
            Assert.That(retry.State.PendingAction?.Attempt, Is.EqualTo(2));
            Assert.That(
                retry.State.PanelTransition?.Status,
                Is.EqualTo(PanelTransitionStatus.Pending));
            Assert.That(staleDeadline.State, Is.SameAs(stateAfterRetry));
            Assert.That(staleDeadline.PublishedView, Is.Null);
            Assert.That(timedOut.Intent, Is.Null);
            Assert.That(timedOut.State.PendingAction, Is.Null);
            Assert.That(
                timedOut.State.PanelTransition?.Status,
                Is.EqualTo(PanelTransitionStatus.TimedOut));
            Assert.That(timedOut.State.PanelTransition?.Attempt, Is.EqualTo(2));
        });
    }

    [Test]
    public void ShouldIgnoreDeadlineAfterSnapshotConfirmation()
    {
        var scenario = CreateRunningScenario(ClientPanel.Inventory);
        var request = scenario.Send(
            new RequestPanelTransitionCommand(
                ClientPanel.TemuairSkills,
                TestPolicy));
        scenario.AdvanceBy(TimeSpan.FromTicks(1));
        var confirmed = scenario.Observe(
            sequence: 2,
            activePanel: ClientPanel.TemuairSkills);
        scenario.AdvanceBy(
            TestPolicy.AttemptTimeout - TimeSpan.FromTicks(1));

        var staleDeadline = scenario.Dispatch(
            request.ScheduledEvents.Single().Input);

        Assert.Multiple(() =>
        {
            Assert.That(
                confirmed.State.PanelTransition?.Status,
                Is.EqualTo(PanelTransitionStatus.Succeeded));
            Assert.That(staleDeadline.State, Is.SameAs(confirmed.State));
            Assert.That(staleDeadline.PublishedView, Is.Null);
        });
    }

    [Test]
    public void ShouldCancelPendingTransitionForLifecycleInterruptions()
    {
        var paused = RunInterruptedScenario(
            scenario => scenario.Pause());
        var stopped = RunInterruptedScenario(
            scenario => scenario.Stop());
        var loggedOut = RunInterruptedScenario(
            scenario => scenario.Observe(
                sequence: 2,
                presence: ClientPresence.LoggedOut,
                activePanel: ClientPanel.Unknown));

        Assert.Multiple(() =>
        {
            Assert.That(paused.PendingAction, Is.Null);
            Assert.That(
                paused.PanelTransition?.Status,
                Is.EqualTo(PanelTransitionStatus.Cancelled));
            Assert.That(paused.Lifecycle, Is.EqualTo(MacroLifecycle.Paused));
            Assert.That(stopped.PendingAction, Is.Null);
            Assert.That(
                stopped.PanelTransition?.Status,
                Is.EqualTo(PanelTransitionStatus.Cancelled));
            Assert.That(stopped.Lifecycle, Is.EqualTo(MacroLifecycle.Stopped));
            Assert.That(loggedOut.PendingAction, Is.Null);
            Assert.That(
                loggedOut.PanelTransition?.Status,
                Is.EqualTo(PanelTransitionStatus.Cancelled));
            Assert.That(
                loggedOut.StopReason,
                Is.EqualTo(MacroStopReason.ClientLoggedOut));
        });
    }

    [Test]
    public void ShouldSupersedePendingTransitionWithDifferentTarget()
    {
        var scenario = CreateRunningScenario(ClientPanel.Inventory);
        var first = scenario.Send(
            new RequestPanelTransitionCommand(
                ClientPanel.TemuairSpells,
                TestPolicy));
        var second = scenario.Send(
            new RequestPanelTransitionCommand(
                ClientPanel.TemuairSkills,
                TestPolicy));

        scenario.AdvanceBy(TestPolicy.AttemptTimeout);
        var staleDeadline = scenario.Dispatch(first.ScheduledEvents.Single().Input);

        Assert.Multiple(() =>
        {
            Assert.That(
                ((SwitchPanelIntent)second.Intent!).ActionId.Value,
                Is.EqualTo(2));
            Assert.That(
                second.State.PanelTransition?.TargetPanel,
                Is.EqualTo(ClientPanel.TemuairSkills));
            Assert.That(staleDeadline.State, Is.SameAs(second.State));
        });
    }

    [Test]
    public void ShouldRejectInvalidPanelTransitionPolicy()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = new PanelTransitionPolicy(
                    TimeSpan.Zero,
                    maximumAttempts: 1));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = new PanelTransitionPolicy(
                    TimeSpan.FromSeconds(1),
                    maximumAttempts: 0));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = new RequestPanelTransitionCommand(
                    ClientPanel.Unknown));
        });
    }

    private static MacroScenario CreateRunningScenario(ClientPanel activePanel)
    {
        var scenario = new MacroScenario();
        scenario.Observe(sequence: 1, activePanel: activePanel);
        scenario.Start();
        return scenario;
    }

    private static MacroState RunInterruptedScenario(
        Func<MacroScenario, MacroDecision> interrupt)
    {
        var scenario = CreateRunningScenario(ClientPanel.Inventory);
        scenario.Send(
            new RequestPanelTransitionCommand(
                ClientPanel.TemuairSpells,
                TestPolicy));

        return interrupt(scenario).State;
    }
}
