using SleepHunter.Runtime.Automation;
using SleepHunter.Runtime.Automation.Panels;
using SleepHunter.Runtime.Commands;
using SleepHunter.Runtime.Engine;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Tests.Scenarios;

namespace SleepHunter.Runtime.Tests.Engine;

public sealed class ObservationChangeScenarioTests
{
    [Test]
    public void ShouldStopOnMapChangeByDefault()
    {
        var scenario = CreateRunningScenario();

        var changed = scenario.Observe(
            sequence: 2,
            location: new MapLocationSnapshot(2, "Abel", 20, 30));

        Assert.Multiple(() =>
        {
            Assert.That(
                changed.State.Lifecycle,
                Is.EqualTo(MacroLifecycle.Stopped));
            Assert.That(
                changed.State.StopReason,
                Is.EqualTo(MacroStopReason.MapChanged));
            Assert.That(
                changed.State.LatestSnapshot?.Location?.MapName,
                Is.EqualTo("Abel"));
            Assert.That(changed.Intent, Is.Null);
        });
    }

    [Test]
    public void ShouldPauseAndCancelPendingActionOnCoordinateChange()
    {
        var scenario = CreateRunningScenario(
            new ObservationChangePolicy(
                coordinateChange: ObservationChangeAction.Pause));
        scenario.Send(
            new RequestPanelTransitionCommand(ClientPanel.TemuairSpells));

        var changed = scenario.Observe(
            sequence: 2,
            activePanel: ClientPanel.Inventory,
            location: new MapLocationSnapshot(1, "Mileth", 21, 30));

        Assert.Multiple(() =>
        {
            Assert.That(
                changed.State.Lifecycle,
                Is.EqualTo(MacroLifecycle.Paused));
            Assert.That(
                changed.State.StopReason,
                Is.EqualTo(MacroStopReason.None));
            Assert.That(changed.State.PendingAction, Is.Null);
            Assert.That(
                changed.State.PanelTransition?.Status,
                Is.EqualTo(PanelTransitionStatus.Cancelled));
            Assert.That(changed.ScheduledEvents, Is.Empty);
        });
    }

    [Test]
    public void ShouldContinueWhenConfiguredForObservationChange()
    {
        var scenario = CreateRunningScenario(
            new ObservationChangePolicy(
                mapChange: ObservationChangeAction.Continue,
                coordinateChange: ObservationChangeAction.Continue));

        var changed = scenario.Observe(
            sequence: 2,
            location: new MapLocationSnapshot(2, "Abel", 21, 31));

        Assert.Multiple(() =>
        {
            Assert.That(
                changed.State.Lifecycle,
                Is.EqualTo(MacroLifecycle.Running));
            Assert.That(
                changed.State.StopReason,
                Is.EqualTo(MacroStopReason.None));
            Assert.That(
                changed.State.LatestSnapshot?.Location?.MapName,
                Is.EqualTo("Abel"));
        });
    }

    [Test]
    public void ShouldPrioritizeMapPolicyWhenMapAndCoordinatesChange()
    {
        var scenario = CreateRunningScenario(
            new ObservationChangePolicy(
                mapChange: ObservationChangeAction.Pause,
                coordinateChange: ObservationChangeAction.Stop));

        var changed = scenario.Observe(
            sequence: 2,
            location: new MapLocationSnapshot(2, "Abel", 21, 31));

        Assert.Multiple(() =>
        {
            Assert.That(
                changed.State.Lifecycle,
                Is.EqualTo(MacroLifecycle.Paused));
            Assert.That(
                changed.State.StopReason,
                Is.EqualTo(MacroStopReason.None));
        });
    }

    [Test]
    public void ShouldRejectUnsupportedObservationChangeActions()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = new ObservationChangePolicy(
                    mapChange: (ObservationChangeAction)int.MaxValue));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = new ObservationChangePolicy(
                    coordinateChange:
                        (ObservationChangeAction)int.MaxValue));
        });
    }

    private static MacroScenario CreateRunningScenario(
        ObservationChangePolicy? policy = null)
    {
        var scenario = new MacroScenario();
        scenario.Send(
            new ConfigureAutomationCommand(
                new AutomationConfiguration(
                    observationChanges:
                        policy ?? ObservationChangePolicy.Default)));
        scenario.Observe(
            sequence: 1,
            activePanel: ClientPanel.Inventory,
            location: new MapLocationSnapshot(1, "Mileth", 20, 30));
        scenario.Start();
        return scenario;
    }
}
