using SleepHunter.Runtime.Automation;
using SleepHunter.Runtime.Automation.Spells;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Tests.Automation.Spells;

public sealed class SpellPlannerTests
{
    [Test]
    public void ShouldReportEmptyQueueWithoutSnapshotSections()
    {
        var plan = SpellPlanner.Plan(
            CreateRequest(
                SpellQueueState.Empty,
                vitals: null,
                spellbook: null));

        Assert.Multiple(() =>
        {
            Assert.That(plan.Status, Is.EqualTo(SpellPlanStatus.QueueEmpty));
            Assert.That(plan.HasSelection, Is.False);
            Assert.That(plan.Readiness, Is.Empty);
        });
    }

    [Test]
    public void ShouldReportMissingSnapshotSections()
    {
        var queue = CreateQueue(CreateEntry(1, "spell"));
        var missingVitals = SpellPlanner.Plan(
            CreateRequest(queue, vitals: null, Spellbook("spell")));
        var missingSpellbook = SpellPlanner.Plan(
            CreateRequest(queue, Vitals(), spellbook: null));

        Assert.Multiple(() =>
        {
            Assert.That(
                missingVitals.Status,
                Is.EqualTo(SpellPlanStatus.SnapshotUnavailable));
            Assert.That(
                missingSpellbook.Status,
                Is.EqualTo(SpellPlanStatus.SnapshotUnavailable));
            Assert.That(missingVitals.HasSelection, Is.False);
            Assert.That(missingSpellbook.HasSelection, Is.False);
        });
    }

    [Test]
    public void ShouldSelectFirstReadyPrioritySpellAndCalculateDuration()
    {
        var missing = CreateEntry(1, "missing");
        var cooling = CreateEntry(2, "cooling");
        var expensive = CreateEntry(3, "expensive");
        var ready = CreateEntry(4, "ready");
        var queue = CreateQueue(missing, cooling, expensive, ready);
        var spellbook = new SpellbookSnapshot(
        [
            Spell("cooling", slot: 1, isActionDelayed: true),
            Spell("expensive", slot: 2, manaCost: 101),
            Spell("ready", slot: 3, castLines: 3)
        ]);

        var plan = SpellPlanner.Plan(
            CreateRequest(queue, Vitals(mana: 100), spellbook));

        Assert.Multiple(() =>
        {
            Assert.That(plan.Status, Is.EqualTo(SpellPlanStatus.Ready));
            Assert.That(plan.SelectedEntry, Is.EqualTo(ready));
            Assert.That(plan.SelectedSpell?.Name, Is.EqualTo("ready"));
            Assert.That(
                plan.CastDuration,
                Is.EqualTo(TimeSpan.FromMilliseconds(3100)));
            Assert.That(
                plan.Readiness.Select(entry => entry.Status),
                Is.EqualTo(new[]
                {
                    SpellReadinessStatus.Missing,
                    SpellReadinessStatus.CoolingDown,
                    SpellReadinessStatus.WaitingForMana,
                    SpellReadinessStatus.Ready
                }));
        });
    }

    [Test]
    public void ShouldKeepSequentialQueueBlockedOnMana()
    {
        var complete = CreateEntry(1, "complete", targetLevel: 5);
        var blocked = CreateEntry(2, "blocked");
        var ready = CreateEntry(3, "ready");
        var queue = CreateQueue(complete, blocked, ready)
            .SetRotation(SpellQueueRotation.Sequential);
        var spellbook = new SpellbookSnapshot(
        [
            Spell("complete", slot: 1, currentLevel: 5),
            Spell("blocked", slot: 2, manaCost: 101),
            Spell("ready", slot: 3)
        ]);

        var plan = SpellPlanner.Plan(
            CreateRequest(queue, Vitals(mana: 100), spellbook));

        Assert.Multiple(() =>
        {
            Assert.That(plan.Status, Is.EqualTo(SpellPlanStatus.Waiting));
            Assert.That(plan.HasSelection, Is.False);
            Assert.That(plan.Queue.Cursor, Is.EqualTo(1));
            Assert.That(
                plan.Readiness[1].Status,
                Is.EqualTo(SpellReadinessStatus.WaitingForMana));
        });
    }

    [Test]
    public void ShouldRotateRoundRobinPastUnavailableSpells()
    {
        var first = CreateEntry(1, "first");
        var cooling = CreateEntry(2, "cooling");
        var third = CreateEntry(3, "third");
        var queue = CreateQueue(first, cooling, third)
            .SetRotation(SpellQueueRotation.RoundRobin);
        var spellbook = new SpellbookSnapshot(
        [
            Spell("first", slot: 1),
            Spell("cooling", slot: 2, isActionDelayed: true),
            Spell("third", slot: 3)
        ]);
        var firstPlan = SpellPlanner.Plan(
            CreateRequest(queue, Vitals(), spellbook));
        var secondPlan = SpellPlanner.Plan(
            CreateRequest(firstPlan.Queue, Vitals(), spellbook));

        Assert.Multiple(() =>
        {
            Assert.That(firstPlan.SelectedEntry, Is.EqualTo(first));
            Assert.That(secondPlan.SelectedEntry, Is.EqualTo(third));
            Assert.That(secondPlan.Queue.Cursor, Is.Zero);
        });
    }

    [TestCase(SpellQueueRotation.Priority)]
    [TestCase(SpellQueueRotation.Sequential)]
    [TestCase(SpellQueueRotation.RoundRobin)]
    public void ShouldBlockOnCoolingSpellWhenSkippingIsDisabled(
        SpellQueueRotation rotation)
    {
        var cooling = CreateEntry(1, "cooling");
        var ready = CreateEntry(2, "ready");
        var queue = CreateQueue(cooling, ready)
            .SetRotation(rotation);
        var spellbook = new SpellbookSnapshot(
        [
            Spell("cooling", slot: 1, isActionDelayed: true),
            Spell("ready", slot: 2)
        ]);
        var policy = new SpellCastPolicy(
            requireMana: true,
            SpellCastTimingPolicy.Default,
            skipCoolingDownSpells: false);

        var plan = SpellPlanner.Plan(
            CreateRequest(
                queue,
                Vitals(),
                spellbook,
                policy: policy));

        Assert.Multiple(() =>
        {
            Assert.That(plan.Status, Is.EqualTo(SpellPlanStatus.Waiting));
            Assert.That(plan.HasSelection, Is.False);
            Assert.That(plan.Queue.Cursor, Is.Zero);
        });
    }

    [Test]
    public void ShouldAllowCastingWithoutRequiredManaPolicy()
    {
        var entry = CreateEntry(1, "spell");
        var policy = new SpellCastPolicy(
            requireMana: false,
            SpellCastTimingPolicy.Default);
        var plan = SpellPlanner.Plan(
            CreateRequest(
                CreateQueue(entry),
                vitals: null,
                Spellbook(Spell("spell", manaCost: 100)),
                policy: policy));

        Assert.Multiple(() =>
        {
            Assert.That(plan.Status, Is.EqualTo(SpellPlanStatus.Ready));
            Assert.That(plan.SelectedEntry, Is.EqualTo(entry));
        });
    }

    [Test]
    public void ShouldHonorQueueEntryHealthConditionWithoutManaPolicy()
    {
        var entry = CreateEntry(
            1,
            "spell",
            healthCondition: new HealthCondition(
                minimumPercentExclusive: 90));
        var queue = CreateQueue(entry);
        var spellbook = Spellbook("spell");
        var policy = new SpellCastPolicy(
            requireMana: false,
            SpellCastTimingPolicy.Default);
        var missingVitals = SpellPlanner.Plan(
            CreateRequest(
                queue,
                vitals: null,
                spellbook,
                policy: policy));
        var blocked = SpellPlanner.Plan(
            CreateRequest(
                queue,
                Vitals(health: 90),
                spellbook,
                policy: policy));
        var ready = SpellPlanner.Plan(
            CreateRequest(
                queue,
                Vitals(health: 91),
                spellbook,
                policy: policy));

        Assert.Multiple(() =>
        {
            Assert.That(
                missingVitals.Status,
                Is.EqualTo(SpellPlanStatus.SnapshotUnavailable));
            Assert.That(blocked.Status, Is.EqualTo(SpellPlanStatus.Waiting));
            Assert.That(
                blocked.Readiness.Single().Status,
                Is.EqualTo(SpellReadinessStatus.WaitingForHealth));
            Assert.That(ready.Status, Is.EqualTo(SpellPlanStatus.Ready));
        });
    }

    [Test]
    public void ShouldHonorLocalCooldownUntilExactReadyTime()
    {
        var entry = CreateEntry(1, "spell");
        var queue = CreateQueue(entry);
        var spellbook = Spellbook("spell");
        var readyAt = new MacroTimestamp(TimeSpan.FromSeconds(5));
        var cooldowns = SpellCooldownState.Empty.WithCooldown(
            entry.Name,
            readyAt);
        var blocked = SpellPlanner.Plan(
            CreateRequest(
                queue,
                Vitals(),
                spellbook,
                cooldowns,
                new MacroTimestamp(TimeSpan.FromSeconds(4))));
        var ready = SpellPlanner.Plan(
            CreateRequest(
                queue,
                Vitals(),
                spellbook,
                cooldowns,
                readyAt));

        Assert.Multiple(() =>
        {
            Assert.That(blocked.Status, Is.EqualTo(SpellPlanStatus.Waiting));
            Assert.That(
                blocked.Readiness.Single().Status,
                Is.EqualTo(SpellReadinessStatus.CoolingDown));
            Assert.That(blocked.Readiness.Single().ReadyAt, Is.EqualTo(readyAt));
            Assert.That(ready.Status, Is.EqualTo(SpellPlanStatus.Ready));
            Assert.That(ready.Cooldowns, Is.EqualTo(SpellCooldownState.Empty));
        });
    }

    [Test]
    public void ShouldDistinguishCompleteAndUnavailableQueues()
    {
        var completed = CreateEntry(1, "completed", targetLevel: 5);
        var unreachable = CreateEntry(2, "unreachable", targetLevel: 101);
        var completePlan = SpellPlanner.Plan(
            CreateRequest(
                CreateQueue(completed),
                Vitals(),
                Spellbook(Spell("completed", currentLevel: 5))));
        var unavailablePlan = SpellPlanner.Plan(
            CreateRequest(
                CreateQueue(unreachable),
                Vitals(),
                Spellbook(Spell(
                    "unreachable",
                    currentLevel: 50,
                    maximumLevel: 100))));

        Assert.Multiple(() =>
        {
            Assert.That(
                completePlan.Status,
                Is.EqualTo(SpellPlanStatus.Complete));
            Assert.That(
                completePlan.Readiness.Single().Status,
                Is.EqualTo(SpellReadinessStatus.Complete));
            Assert.That(
                unavailablePlan.Status,
                Is.EqualTo(SpellPlanStatus.Unavailable));
            Assert.That(
                unavailablePlan.Readiness.Single().Status,
                Is.EqualTo(SpellReadinessStatus.TargetLevelUnavailable));
        });
    }

    [Test]
    public void ShouldReportUnavailableWhenQueuedSpellIsMissing()
    {
        var plan = SpellPlanner.Plan(
            CreateRequest(
                CreateQueue(CreateEntry(1, "missing")),
                Vitals(),
                SpellbookSnapshot.Empty));

        Assert.Multiple(() =>
        {
            Assert.That(plan.Status, Is.EqualTo(SpellPlanStatus.Unavailable));
            Assert.That(
                plan.Readiness.Single().Status,
                Is.EqualTo(SpellReadinessStatus.Missing));
        });
    }

    private static SpellPlanningRequest CreateRequest(
        SpellQueueState queue,
        VitalsSnapshot? vitals,
        SpellbookSnapshot? spellbook,
        SpellCooldownState? cooldowns = null,
        MacroTimestamp? currentTime = null,
        SpellCastPolicy? policy = null) =>
        new(
            queue,
            vitals,
            spellbook,
            cooldowns,
            currentTime ?? MacroTimestamp.Zero,
            policy);

    private static SpellQueueState CreateQueue(
        params SpellQueueEntry[] entries)
    {
        var queue = SpellQueueState.Empty;
        foreach (var entry in entries)
        {
            queue = queue.Add(entry);
        }

        return queue;
    }

    private static SpellQueueEntry CreateEntry(
        long id,
        string name,
        int? targetLevel = null,
        HealthCondition? healthCondition = null) =>
        new(
            new SpellQueueEntryId(id),
            name,
            targetLevel,
            healthCondition: healthCondition);

    private static VitalsSnapshot Vitals(
        int mana = 100,
        int health = 100) =>
        new(
            currentHealth: health,
            maximumHealth: 100,
            currentMana: mana,
            maximumMana: 100);

    private static SpellbookSnapshot Spellbook(string spellName) =>
        Spellbook(Spell(spellName));

    private static SpellbookSnapshot Spellbook(params SpellSnapshot[] spells) =>
        new(spells);

    private static SpellSnapshot Spell(
        string name,
        int slot = 1,
        int currentLevel = 0,
        int maximumLevel = 100,
        int castLines = 1,
        int manaCost = 0,
        bool isActionDelayed = false) =>
        new(
            name,
            slot,
            currentLevel,
            maximumLevel,
            castLines,
            manaCost,
            cooldown: TimeSpan.Zero,
            isActionDelayed);
}
