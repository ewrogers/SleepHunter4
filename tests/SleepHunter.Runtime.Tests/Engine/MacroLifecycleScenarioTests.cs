using SleepHunter.Runtime.Engine;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Tests.Scenarios;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Tests.Engine;

public sealed class MacroLifecycleScenarioTests
{
    [Test]
    public void ShouldApplyLifecycleCommandsDeterministically()
    {
        var scenario = new MacroScenario();

        scenario.Observe(1);
        scenario.Start();
        scenario.AdvanceBy(TimeSpan.FromSeconds(1));
        scenario.Pause();
        scenario.AdvanceBy(TimeSpan.FromSeconds(2));
        scenario.Resume();
        scenario.Stop();

        var publishedViews = scenario.Decisions
            .Select(decision => decision.PublishedView)
            .OfType<MacroViewSnapshot>()
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(
                publishedViews.Select(view => view.Revision),
                Is.EqualTo(new long[] { 1, 2, 3, 4, 5 }));
            Assert.That(
                publishedViews.Select(view => view.Lifecycle),
                Is.EqualTo(new[]
                {
                    MacroLifecycle.Stopped,
                    MacroLifecycle.Running,
                    MacroLifecycle.Paused,
                    MacroLifecycle.Running,
                    MacroLifecycle.Stopped
                }));
            Assert.That(
                publishedViews.Select(view => view.StopReason),
                Is.EqualTo(new[]
                {
                    MacroStopReason.None,
                    MacroStopReason.None,
                    MacroStopReason.None,
                    MacroStopReason.None,
                    MacroStopReason.UserRequested
                }));
            Assert.That(
                scenario.Decisions.Select(decision => decision.Effect),
                Is.All.Null);
        });
    }

    [Test]
    public void ShouldIgnoreInvalidStaleAndForeignSnapshots()
    {
        var scenario = new MacroScenario();
        scenario.Observe(2);
        var acceptedState = scenario.State;

        var partial = scenario.Observe(3, SnapshotQuality.Partial);
        var stale = scenario.Observe(1);
        var foreign = scenario.Observe(
            4,
            client: new ClientIdentity("other-client", "test"));

        Assert.Multiple(() =>
        {
            Assert.That(partial.State, Is.SameAs(acceptedState));
            Assert.That(stale.State, Is.SameAs(acceptedState));
            Assert.That(foreign.State, Is.SameAs(acceptedState));
            Assert.That(partial.PublishedView, Is.Null);
            Assert.That(stale.PublishedView, Is.Null);
            Assert.That(foreign.PublishedView, Is.Null);
            Assert.That(scenario.State.LatestSnapshot?.Sequence.Value, Is.EqualTo(2));
        });
    }

    [Test]
    public void ShouldStopWhenNewSnapshotShowsClientLoggedOut()
    {
        var scenario = new MacroScenario();
        scenario.Observe(1);
        scenario.Start();
        scenario.AdvanceBy(TimeSpan.FromMilliseconds(50));

        var logout = scenario.Observe(2, presence: ClientPresence.LoggedOut);
        var rejectedRestart = scenario.Start();
        scenario.Observe(3, presence: ClientPresence.InWorld);
        var restart = scenario.Start();

        Assert.Multiple(() =>
        {
            Assert.That(logout.State.Lifecycle, Is.EqualTo(MacroLifecycle.Stopped));
            Assert.That(
                logout.State.StopReason,
                Is.EqualTo(MacroStopReason.ClientLoggedOut));
            Assert.That(
                logout.State.LastTransitionAt,
                Is.EqualTo(new MacroTimestamp(TimeSpan.FromMilliseconds(50))));
            Assert.That(rejectedRestart.PublishedView, Is.Null);
            Assert.That(restart.State.Lifecycle, Is.EqualTo(MacroLifecycle.Running));
            Assert.That(restart.State.StopReason, Is.EqualTo(MacroStopReason.None));
        });
    }

    [Test]
    public void ShouldIgnoreSnapshotCapturedAfterCurrentEventTime()
    {
        var scenario = new MacroScenario();

        var decision = scenario.Observe(
            1,
            captureCompletedAt: new MacroTimestamp(TimeSpan.FromMilliseconds(1)));

        Assert.Multiple(() =>
        {
            Assert.That(decision.State, Is.SameAs(MacroState.Initial));
            Assert.That(decision.PublishedView, Is.Null);
        });
    }

    [Test]
    public void ShouldNotPublishForInapplicableLifecycleCommands()
    {
        var scenario = new MacroScenario();

        var start = scenario.Start();
        var pause = scenario.Pause();
        var resume = scenario.Resume();
        var stop = scenario.Stop();

        Assert.Multiple(() =>
        {
            Assert.That(start.State, Is.SameAs(MacroState.Initial));
            Assert.That(pause.State, Is.SameAs(MacroState.Initial));
            Assert.That(resume.State, Is.SameAs(MacroState.Initial));
            Assert.That(stop.State, Is.SameAs(MacroState.Initial));
            Assert.That(
                scenario.Decisions.Select(decision => decision.PublishedView),
                Is.All.Null);
        });
    }

    [Test]
    public void ShouldProduceIdenticalDecisionsForIdenticalScenarios()
    {
        var first = RunLifecycleScenario();
        var second = RunLifecycleScenario();

        Assert.That(first, Is.EqualTo(second));
    }

    private static MacroDecision[] RunLifecycleScenario()
    {
        var scenario = new MacroScenario();
        scenario.Observe(1);
        scenario.Start();
        scenario.AdvanceBy(TimeSpan.FromMilliseconds(10));
        scenario.Pause();
        scenario.AdvanceBy(TimeSpan.FromMilliseconds(20));
        scenario.Resume();
        scenario.Stop();

        return scenario.Decisions.ToArray();
    }
}
