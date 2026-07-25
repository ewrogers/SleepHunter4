using SleepHunter.Runtime.Automation.WaterBeds;
using SleepHunter.Runtime.Commands;
using SleepHunter.Runtime.Intents;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Tests.Scenarios;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Tests.Engine;

public sealed class WaterBedScenarioTests
{
    private static readonly TimeSpan ActionDuration =
        TimeSpan.FromMilliseconds(10);

    private static readonly TimeSpan MinimumInterval =
        TimeSpan.FromMilliseconds(500);

    [Test]
    public void ShouldClickMapAwareTileAndCompleteWithoutRetry()
    {
        var policy = Policy(targetX: 55, targetY: 45);
        var scenario = CreateRunningScenario(
            currentMana: 999,
            location: Location());

        var requested = scenario.Send(new UseWaterBedCommand(policy));
        var intent = (ClickTileIntent)requested.Intent!;
        var deadline = requested.ScheduledEvents.Single();
        scenario.AdvanceBy(ActionDuration);
        var completed = scenario.Dispatch(deadline.Input);

        Assert.Multiple(() =>
        {
            Assert.That(intent.Target.MapNumber, Is.EqualTo(1));
            Assert.That(intent.Target.MapName, Is.EqualTo("test map"));
            Assert.That(intent.Target.X, Is.EqualTo(55));
            Assert.That(intent.Target.Y, Is.EqualTo(45));
            Assert.That(
                requested.State.WaterBed?.Status,
                Is.EqualTo(WaterBedStatus.Clicking));
            Assert.That(
                requested.State.WaterBed?.ReadyAt,
                Is.EqualTo(
                    MacroTimestamp.Zero.Add(MinimumInterval)));
            Assert.That(
                requested.State.PendingAction?.MaximumAttempts,
                Is.EqualTo(1));
            Assert.That(
                completed.State.WaterBed?.Status,
                Is.EqualTo(WaterBedStatus.Succeeded));
            Assert.That(completed.State.PendingAction, Is.Null);
            Assert.That(completed.Intent, Is.Null);
            Assert.That(completed.ScheduledEvents, Is.Empty);
        });
    }

    [Test]
    public void ShouldWaitForThrottleAndFreshPostClickSnapshot()
    {
        var policy = Policy(targetX: 50, targetY: 50);
        var scenario = CreateRunningScenario(
            currentMana: 0,
            location: Location());

        var first = scenario.Send(new UseWaterBedCommand(policy));
        scenario.AdvanceBy(ActionDuration);
        scenario.Dispatch(first.ScheduledEvents.Single().Input);
        scenario.AdvanceBy(
            MinimumInterval - ActionDuration);

        var stale = scenario.Send(new UseWaterBedCommand(policy));
        scenario.AdvanceBy(TimeSpan.FromTicks(1));
        scenario.Observe(
            sequence: 2,
            vitals: Vitals(currentMana: 0),
            location: Location());
        var fresh = scenario.Send(new UseWaterBedCommand(policy));

        Assert.Multiple(() =>
        {
            Assert.That(stale.Intent, Is.Null);
            Assert.That(
                stale.State.WaterBed?.Status,
                Is.EqualTo(WaterBedStatus.SnapshotUnavailable));
            Assert.That(fresh.Intent, Is.TypeOf<ClickTileIntent>());
            Assert.That(
                fresh.State.WaterBed?.Status,
                Is.EqualTo(WaterBedStatus.Clicking));
        });
    }

    [Test]
    public void ShouldRemainCoolingUntilExactInterval()
    {
        var policy = Policy(targetX: 50, targetY: 50);
        var scenario = CreateRunningScenario(
            currentMana: 0,
            location: Location());

        var first = scenario.Send(new UseWaterBedCommand(policy));
        scenario.AdvanceBy(ActionDuration);
        scenario.Dispatch(first.ScheduledEvents.Single().Input);
        scenario.AdvanceBy(TimeSpan.FromTicks(1));
        scenario.Observe(
            sequence: 2,
            vitals: Vitals(currentMana: 0),
            location: Location());

        var cooling = scenario.Send(new UseWaterBedCommand(policy));
        scenario.AdvanceBy(
            MinimumInterval -
            ActionDuration -
            TimeSpan.FromTicks(1));
        var ready = scenario.Send(new UseWaterBedCommand(policy));

        Assert.Multiple(() =>
        {
            Assert.That(cooling.Intent, Is.Null);
            Assert.That(
                cooling.State.WaterBed?.Status,
                Is.EqualTo(WaterBedStatus.CoolingDown));
            Assert.That(ready.Intent, Is.TypeOf<ClickTileIntent>());
        });
    }

    [Test]
    public void ShouldPublishStableNonActionOutcomes()
    {
        var sufficient = CreateRunningScenario(
            currentMana: 1000,
            location: Location());
        var outOfRange = CreateRunningScenario(
            currentMana: 0,
            location: Location());

        var sufficientDecision = sufficient.Send(
            new UseWaterBedCommand(
                Policy(targetX: 50, targetY: 50)));
        var outOfRangeDecision = outOfRange.Send(
            new UseWaterBedCommand(
                Policy(targetX: 50, targetY: 61)));

        Assert.Multiple(() =>
        {
            Assert.That(sufficientDecision.Intent, Is.Null);
            Assert.That(
                sufficientDecision.State.WaterBed?.Status,
                Is.EqualTo(WaterBedStatus.ManaSufficient));
            Assert.That(outOfRangeDecision.Intent, Is.Null);
            Assert.That(
                outOfRangeDecision.State.WaterBed?.Status,
                Is.EqualTo(WaterBedStatus.OutOfRange));
        });
    }

    [Test]
    public void ShouldCancelPendingClickOnPauseAndIgnoreOldDeadline()
    {
        var scenario = CreateRunningScenario(
            currentMana: 0,
            location: Location());
        var requested = scenario.Send(
            new UseWaterBedCommand(
                Policy(targetX: 50, targetY: 50)));

        var paused = scenario.Pause();
        scenario.AdvanceBy(ActionDuration);
        var staleDeadline = scenario.Dispatch(
            requested.ScheduledEvents.Single().Input);

        Assert.Multiple(() =>
        {
            Assert.That(paused.State.PendingAction, Is.Null);
            Assert.That(
                paused.State.WaterBed?.Status,
                Is.EqualTo(WaterBedStatus.Cancelled));
            Assert.That(staleDeadline.State, Is.SameAs(paused.State));
        });
    }

    private static MacroScenario CreateRunningScenario(
        int currentMana,
        MapLocationSnapshot? location)
    {
        var scenario = new MacroScenario();
        scenario.Observe(
            sequence: 1,
            vitals: Vitals(currentMana),
            location: location);
        scenario.Start();
        return scenario;
    }

    private static WaterBedPolicy Policy(int targetX, int targetY) =>
        new(
            targetX,
            targetY,
            manaThreshold: 1000,
            maximumXDistance: 10,
            maximumYDistance: 10,
            minimumInterval: MinimumInterval,
            actionDuration: ActionDuration);

    private static MapLocationSnapshot Location() =>
        new(
            mapNumber: 1,
            mapName: "test map",
            x: 50,
            y: 50);

    private static VitalsSnapshot Vitals(int currentMana) =>
        new(
            currentHealth: 100,
            maximumHealth: 100,
            currentMana,
            maximumMana: 1000);
}
