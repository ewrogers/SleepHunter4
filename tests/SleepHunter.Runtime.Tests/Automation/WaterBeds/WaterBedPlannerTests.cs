using SleepHunter.Runtime.Automation.WaterBeds;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Tests.Automation.WaterBeds;

public sealed class WaterBedPlannerTests
{
    [Test]
    public void ShouldSelectLowManaTargetOnCurrentMap()
    {
        var policy = new WaterBedPolicy(
            targetX: 55,
            targetY: 45,
            manaThreshold: 1000);

        var plan = WaterBedPlanner.Plan(
            Request(
                policy,
                location: Location(50, 50),
                vitals: Vitals(currentMana: 999)));

        Assert.Multiple(() =>
        {
            Assert.That(plan.Status, Is.EqualTo(WaterBedPlanStatus.Ready));
            Assert.That(plan.Target?.MapNumber, Is.EqualTo(1));
            Assert.That(plan.Target?.MapName, Is.EqualTo("test map"));
            Assert.That(plan.Target?.X, Is.EqualTo(55));
            Assert.That(plan.Target?.Y, Is.EqualTo(45));
        });
    }

    [Test]
    public void ShouldRequireManaStrictlyBelowThreshold()
    {
        var policy = new WaterBedPolicy(
            targetX: 50,
            targetY: 50,
            manaThreshold: 1000);

        var plan = WaterBedPlanner.Plan(
            Request(
                policy,
                location: Location(),
                vitals: Vitals(currentMana: 1000)));

        Assert.That(
            plan.Status,
            Is.EqualTo(WaterBedPlanStatus.ManaSufficient));
    }

    [Test]
    public void ShouldRejectTargetOutsideEitherAxis()
    {
        var xPolicy = new WaterBedPolicy(
            targetX: 61,
            targetY: 50,
            maximumXDistance: 10,
            maximumYDistance: 10);
        var yPolicy = new WaterBedPolicy(
            targetX: 50,
            targetY: 61,
            maximumXDistance: 10,
            maximumYDistance: 10);

        var xPlan = WaterBedPlanner.Plan(
            Request(
                xPolicy,
                location: Location(),
                vitals: Vitals(currentMana: 0)));
        var yPlan = WaterBedPlanner.Plan(
            Request(
                yPolicy,
                location: Location(),
                vitals: Vitals(currentMana: 0)));

        Assert.Multiple(() =>
        {
            Assert.That(
                xPlan.Status,
                Is.EqualTo(WaterBedPlanStatus.OutOfRange));
            Assert.That(
                yPlan.Status,
                Is.EqualTo(WaterBedPlanStatus.OutOfRange));
        });
    }

    [Test]
    public void ShouldBecomeReadyAtExactMonotonicDeadline()
    {
        var policy = new WaterBedPolicy(targetX: 50, targetY: 50);
        var readyAt = new MacroTimestamp(TimeSpan.FromMilliseconds(500));

        var waiting = WaterBedPlanner.Plan(
            Request(
                policy,
                location: Location(),
                vitals: Vitals(currentMana: 0),
                readyAt,
                currentTime: new MacroTimestamp(
                    readyAt.Elapsed - TimeSpan.FromTicks(1))));
        var ready = WaterBedPlanner.Plan(
            Request(
                policy,
                location: Location(),
                vitals: Vitals(currentMana: 0),
                readyAt,
                currentTime: readyAt));

        Assert.Multiple(() =>
        {
            Assert.That(
                waiting.Status,
                Is.EqualTo(WaterBedPlanStatus.CoolingDown));
            Assert.That(
                ready.Status,
                Is.EqualTo(WaterBedPlanStatus.Ready));
        });
    }

    [Test]
    public void ShouldRequireCompleteFreshSnapshotSections()
    {
        var policy = new WaterBedPolicy(targetX: 50, targetY: 50);

        var missingLocation = WaterBedPlanner.Plan(
            Request(
                policy,
                location: null,
                vitals: Vitals(currentMana: 0)));
        var missingVitals = WaterBedPlanner.Plan(
            Request(
                policy,
                location: Location(),
                vitals: null));
        var stale = WaterBedPlanner.Plan(
            Request(
                policy,
                location: Location(),
                vitals: Vitals(currentMana: 0),
                snapshotIsFresh: false));

        Assert.Multiple(() =>
        {
            Assert.That(
                missingLocation.Status,
                Is.EqualTo(WaterBedPlanStatus.SnapshotUnavailable));
            Assert.That(
                missingVitals.Status,
                Is.EqualTo(WaterBedPlanStatus.SnapshotUnavailable));
            Assert.That(
                stale.Status,
                Is.EqualTo(WaterBedPlanStatus.SnapshotUnavailable));
        });
    }

    [Test]
    public void ShouldValidatePolicyBoundaries()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                () => new WaterBedPolicy(targetX: -1, targetY: 0),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => new WaterBedPolicy(targetX: 0, targetY: -1),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => new WaterBedPolicy(
                    targetX: 0,
                    targetY: 0,
                    manaThreshold: -1),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => new WaterBedPolicy(
                    targetX: 0,
                    targetY: 0,
                    maximumXDistance: -1),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => new WaterBedPolicy(
                    targetX: 0,
                    targetY: 0,
                    maximumYDistance: -1),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => new WaterBedPolicy(
                    targetX: 0,
                    targetY: 0,
                    minimumInterval: TimeSpan.Zero),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => new WaterBedPolicy(
                    targetX: 0,
                    targetY: 0,
                    actionDuration: TimeSpan.Zero),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }

    private static WaterBedPlanningRequest Request(
        WaterBedPolicy policy,
        MapLocationSnapshot? location,
        VitalsSnapshot? vitals,
        MacroTimestamp? readyAt = null,
        MacroTimestamp? currentTime = null,
        bool snapshotIsFresh = true) =>
        new(
            location,
            vitals,
            readyAt,
            currentTime ?? MacroTimestamp.Zero,
            policy,
            snapshotIsFresh);

    private static MapLocationSnapshot Location(
        int x = 50,
        int y = 50) =>
        new(
            mapNumber: 1,
            mapName: "test map",
            x,
            y);

    private static VitalsSnapshot Vitals(int currentMana) =>
        new(
            currentHealth: 100,
            maximumHealth: 100,
            currentMana,
            maximumMana: 1000);
}
