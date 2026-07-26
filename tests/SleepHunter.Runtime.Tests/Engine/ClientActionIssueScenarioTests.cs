using SleepHunter.Runtime.Actions;
using SleepHunter.Runtime.Automation.Panels;
using SleepHunter.Runtime.Commands;
using SleepHunter.Runtime.Engine;
using SleepHunter.Runtime.Events;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Tests.Scenarios;

namespace SleepHunter.Runtime.Tests.Engine;

public sealed class ClientActionIssueScenarioTests
{
    private static readonly PanelTransitionPolicy TestPolicy = new(
        TimeSpan.FromMilliseconds(100),
        maximumAttempts: 2);

    [Test]
    public void ShouldMarkPendingActionIssuedExactlyOnce()
    {
        var scenario = CreateScenario();
        var requested = RequestPanel(scenario);
        var actionId = requested.State.PendingAction!.Intent.ActionId;

        var issued = scenario.Dispatch(
            Issue(actionId, ClientActionIssueStatus.Issued));
        var duplicate = scenario.Dispatch(
            Issue(actionId, ClientActionIssueStatus.Issued));

        Assert.Multiple(() =>
        {
            Assert.That(requested.State.PendingAction!.IsIssued, Is.False);
            Assert.That(issued.State.PendingAction!.IsIssued, Is.True);
            Assert.That(
                issued.State.LastActionIssue?.Status,
                Is.EqualTo(ClientActionIssueStatus.Issued));
            Assert.That(
                issued.State.Lifecycle,
                Is.EqualTo(MacroLifecycle.Running));
            Assert.That(duplicate.State, Is.SameAs(issued.State));
            Assert.That(duplicate.PublishedView, Is.Null);
        });
    }

    [Test]
    public void ShouldConfirmOnlyFromASnapshotCapturedAfterIssuance()
    {
        var scenario = CreateScenario();
        var requested = RequestPanel(scenario);
        var actionId = requested.State.PendingAction!.Intent.ActionId;
        scenario.AdvanceBy(TimeSpan.FromTicks(1));

        var beforeIssue = scenario.Observe(
            sequence: 2,
            activePanel: ClientPanel.TemuairSpells);
        var issued = scenario.Dispatch(
            Issue(actionId, ClientActionIssueStatus.Issued));
        var capturedAtIssue = scenario.Observe(
            sequence: 3,
            activePanel: ClientPanel.TemuairSpells);
        scenario.AdvanceBy(TimeSpan.FromTicks(1));
        var confirmed = scenario.Observe(
            sequence: 4,
            activePanel: ClientPanel.TemuairSpells);

        Assert.Multiple(() =>
        {
            Assert.That(beforeIssue.State.PendingAction, Is.Not.Null);
            Assert.That(issued.State.PendingAction?.IsIssued, Is.True);
            Assert.That(capturedAtIssue.State.PendingAction, Is.Not.Null);
            Assert.That(confirmed.State.PendingAction, Is.Null);
            Assert.That(
                confirmed.State.PanelTransition?.Status,
                Is.EqualTo(PanelTransitionStatus.Succeeded));
        });
    }

    [TestCase(ClientActionIssueStatus.Rejected)]
    [TestCase(ClientActionIssueStatus.Unsupported)]
    [TestCase(ClientActionIssueStatus.Failed)]
    [TestCase(ClientActionIssueStatus.PartiallyIssued)]
    public void ShouldPauseAndClearActionWhenIssuanceDoesNotSucceed(
        ClientActionIssueStatus status)
    {
        var scenario = CreateScenario();
        var requested = RequestPanel(scenario);
        var actionId = requested.State.PendingAction!.Intent.ActionId;

        var failed = scenario.Dispatch(Issue(actionId, status));

        Assert.Multiple(() =>
        {
            Assert.That(failed.State.Lifecycle, Is.EqualTo(MacroLifecycle.Paused));
            Assert.That(failed.State.PendingAction, Is.Null);
            Assert.That(
                failed.State.PanelTransition?.Status,
                Is.EqualTo(PanelTransitionStatus.IssueFailed));
            Assert.That(failed.State.LastActionIssue?.ActionId, Is.EqualTo(actionId));
            Assert.That(failed.State.LastActionIssue?.Status, Is.EqualTo(status));
        });
    }

    [Test]
    public void ShouldPauseWhenIssuanceFeedbackMissesActionDeadline()
    {
        var scenario = CreateScenario();
        var requested = RequestPanel(scenario);
        scenario.AdvanceBy(TestPolicy.AttemptTimeout);

        var timedOut = scenario.Dispatch(
            requested.ScheduledEvents.Single().Input);

        Assert.Multiple(() =>
        {
            Assert.That(
                timedOut.State.Lifecycle,
                Is.EqualTo(MacroLifecycle.Paused));
            Assert.That(timedOut.State.PendingAction, Is.Null);
            Assert.That(
                timedOut.State.LastActionIssue?.Status,
                Is.EqualTo(ClientActionIssueStatus.TimedOut));
            Assert.That(
                timedOut.State.PanelTransition?.Status,
                Is.EqualTo(PanelTransitionStatus.IssueFailed));
        });
    }

    [Test]
    public void ShouldTreatLateIssuedFeedbackAsTimedOut()
    {
        var scenario = CreateScenario();
        var requested = RequestPanel(scenario);
        var actionId = requested.State.PendingAction!.Intent.ActionId;
        scenario.AdvanceBy(TestPolicy.AttemptTimeout);

        var timedOut = scenario.Dispatch(
            Issue(actionId, ClientActionIssueStatus.Issued));

        Assert.Multiple(() =>
        {
            Assert.That(timedOut.State.Lifecycle, Is.EqualTo(MacroLifecycle.Paused));
            Assert.That(timedOut.State.PendingAction, Is.Null);
            Assert.That(
                timedOut.State.LastActionIssue?.Status,
                Is.EqualTo(ClientActionIssueStatus.TimedOut));
            Assert.That(
                timedOut.State.PanelTransition?.Status,
                Is.EqualTo(PanelTransitionStatus.IssueFailed));
        });
    }

    [Test]
    public void ShouldIgnoreStaleIssuanceFeedback()
    {
        var scenario = CreateScenario();
        var requested = RequestPanel(scenario);

        var stale = scenario.Dispatch(
            Issue(
                new ClientActionId(99),
                ClientActionIssueStatus.Failed));

        Assert.Multiple(() =>
        {
            Assert.That(stale.State, Is.SameAs(requested.State));
            Assert.That(stale.PublishedView, Is.Null);
            Assert.That(stale.State.LastActionIssue, Is.Null);
        });
    }

    [Test]
    public void ShouldValidateClientActionIssueValues()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                () => new ClientActionIssue(
                    default,
                    ClientActionIssueStatus.Issued),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => new ClientActionIssue(
                    new ClientActionId(1),
                    (ClientActionIssueStatus)int.MaxValue),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }

    private static MacroScenario CreateScenario()
    {
        var scenario = new MacroScenario(issueActions: false);
        scenario.Observe(
            sequence: 1,
            activePanel: ClientPanel.Inventory);
        scenario.Start();
        return scenario;
    }

    private static MacroDecision RequestPanel(MacroScenario scenario) =>
        scenario.Send(
            new RequestPanelTransitionCommand(
                ClientPanel.TemuairSpells,
                TestPolicy));

    private static ClientActionIssueObserved Issue(
        ClientActionId actionId,
        ClientActionIssueStatus status) =>
        new(new ClientActionIssue(actionId, status));
}
