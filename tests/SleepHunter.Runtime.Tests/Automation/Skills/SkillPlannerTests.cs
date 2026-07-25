using SleepHunter.Runtime.Automation;
using SleepHunter.Runtime.Automation.Skills;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Tests.Automation.Skills;

public sealed class SkillPlannerTests
{
    [Test]
    public void ShouldReportEmptyQueueWithoutSnapshotSections()
    {
        var plan = SkillPlanner.Plan(
            Request(
                SkillQueueState.Empty,
                vitals: null,
                skillbook: null));

        Assert.Multiple(() =>
        {
            Assert.That(plan.Status, Is.EqualTo(SkillPlanStatus.QueueEmpty));
            Assert.That(plan.HasSelection, Is.False);
            Assert.That(plan.Readiness, Is.Empty);
        });
    }

    [Test]
    public void ShouldReportMissingRequiredSnapshotSections()
    {
        var queue = Queue(Entry(1, "skill"));
        var missingBook = SkillPlanner.Plan(
            Request(queue, Vitals(), skillbook: null));
        var missingVitals = SkillPlanner.Plan(
            Request(queue, vitals: null, Skillbook(Skill("skill"))));

        Assert.Multiple(() =>
        {
            Assert.That(
                missingBook.Status,
                Is.EqualTo(SkillPlanStatus.SnapshotUnavailable));
            Assert.That(
                missingVitals.Status,
                Is.EqualTo(SkillPlanStatus.SnapshotUnavailable));
        });
    }

    [Test]
    public void ShouldNotRequireVitalsWhenPolicyAndSkillsDoNotUseThem()
    {
        var policy = new SkillUsePolicy(requireMana: false);
        var entry = Entry(1, "skill");

        var plan = SkillPlanner.Plan(
            Request(
                Queue(entry),
                vitals: null,
                Skillbook(Skill("skill")),
                policy: policy));

        Assert.Multiple(() =>
        {
            Assert.That(plan.Status, Is.EqualTo(SkillPlanStatus.Ready));
            Assert.That(plan.SelectedEntry, Is.EqualTo(entry));
        });
    }

    [Test]
    public void ShouldClassifyReadinessAndSelectFirstReadySkill()
    {
        var missing = Entry(1, "missing");
        var cooling = Entry(2, "cooling");
        var health = Entry(3, "health");
        var mana = Entry(4, "mana");
        var ready = Entry(5, "ready");
        var plan = SkillPlanner.Plan(
            Request(
                Queue(missing, cooling, health, mana, ready),
                Vitals(health: 50, mana: 50),
                Skillbook(
                    Skill("cooling", slot: 1, isActionDelayed: true),
                    Skill(
                        "health",
                        slot: 2,
                        healthCondition: new HealthCondition(
                            minimumPercentExclusive: 90)),
                    Skill("mana", slot: 3, manaCost: 51),
                    Skill("ready", slot: 4))));

        Assert.Multiple(() =>
        {
            Assert.That(plan.Status, Is.EqualTo(SkillPlanStatus.Ready));
            Assert.That(plan.SelectedEntry, Is.EqualTo(ready));
            Assert.That(plan.SelectedSkill?.Name, Is.EqualTo("ready"));
            Assert.That(
                plan.Readiness[0].Status,
                Is.EqualTo(SkillReadinessStatus.Missing));
            Assert.That(
                plan.Readiness[1].Status,
                Is.EqualTo(SkillReadinessStatus.CoolingDown));
            Assert.That(
                plan.Readiness[2].Status,
                Is.EqualTo(SkillReadinessStatus.WaitingForHealth));
            Assert.That(
                plan.Readiness[3].Status,
                Is.EqualTo(SkillReadinessStatus.WaitingForMana));
            Assert.That(
                plan.Readiness[4].Status,
                Is.EqualTo(SkillReadinessStatus.Ready));
        });
    }

    [Test]
    public void ShouldRotateReadySkillsAndSkipTemporarilyUnavailableSkills()
    {
        var first = Entry(1, "first");
        var cooling = Entry(2, "cooling");
        var third = Entry(3, "third");
        var queue = Queue(first, cooling, third);
        var skillbook = Skillbook(
            Skill("first", slot: 1),
            Skill("cooling", slot: 2, isActionDelayed: true),
            Skill("third", slot: 3));

        var firstPlan = SkillPlanner.Plan(
            Request(queue, Vitals(), skillbook));
        var secondPlan = SkillPlanner.Plan(
            Request(firstPlan.Queue, Vitals(), skillbook));

        Assert.Multiple(() =>
        {
            Assert.That(firstPlan.SelectedEntry, Is.EqualTo(first));
            Assert.That(secondPlan.SelectedEntry, Is.EqualTo(third));
            Assert.That(secondPlan.Queue.Cursor, Is.Zero);
        });
    }

    [Test]
    public void ShouldHonorLocalCooldownUntilExactReadyTime()
    {
        var entry = Entry(1, "skill");
        var queue = Queue(entry);
        var skillbook = Skillbook(Skill("skill"));
        var readyAt = new MacroTimestamp(TimeSpan.FromSeconds(5));
        var cooldowns = SkillCooldownState.Empty.WithCooldown(
            entry.Name,
            readyAt);
        var blocked = SkillPlanner.Plan(
            Request(
                queue,
                Vitals(),
                skillbook,
                cooldowns,
                new MacroTimestamp(TimeSpan.FromSeconds(4))));
        var ready = SkillPlanner.Plan(
            Request(queue, Vitals(), skillbook, cooldowns, readyAt));

        Assert.Multiple(() =>
        {
            Assert.That(blocked.Status, Is.EqualTo(SkillPlanStatus.Waiting));
            Assert.That(
                blocked.Readiness.Single().Status,
                Is.EqualTo(SkillReadinessStatus.CoolingDown));
            Assert.That(blocked.Readiness.Single().ReadyAt, Is.EqualTo(readyAt));
            Assert.That(ready.Status, Is.EqualTo(SkillPlanStatus.Ready));
            Assert.That(ready.Cooldowns, Is.EqualTo(SkillCooldownState.Empty));
        });
    }

    [Test]
    public void ShouldDeriveSpaceAssailAndDisarmRequirements()
    {
        var entry = Entry(1, "assail");
        var skillbook = Skillbook(Skill("assail", isAssail: true));
        var defaultPlan = SkillPlanner.Plan(
            Request(Queue(entry), Vitals(), skillbook));
        var slotPolicy = new SkillUsePolicy(
            assailMode: AssailMode.SkillSlot,
            disarmForAssails: false);
        var slotPlan = SkillPlanner.Plan(
            Request(
                Queue(entry),
                Vitals(),
                skillbook,
                policy: slotPolicy));

        Assert.Multiple(() =>
        {
            Assert.That(
                defaultPlan.ActionKind,
                Is.EqualTo(SkillActionKind.Assail));
            Assert.That(defaultPlan.RequiresDisarm, Is.True);
            Assert.That(
                slotPlan.ActionKind,
                Is.EqualTo(SkillActionKind.UseSkill));
            Assert.That(slotPlan.RequiresDisarm, Is.False);
        });
    }

    [Test]
    public void ShouldPreserveExplicitDisarmAndDialogMetadata()
    {
        var entry = Entry(1, "skill");
        var plan = SkillPlanner.Plan(
            Request(
                Queue(entry),
                Vitals(),
                Skillbook(
                    Skill(
                        "skill",
                        requiresDisarm: true,
                        opensDialog: true))));

        Assert.Multiple(() =>
        {
            Assert.That(
                plan.ActionKind,
                Is.EqualTo(SkillActionKind.UseSkill));
            Assert.That(plan.RequiresDisarm, Is.True);
            Assert.That(plan.SelectedSkill?.OpensDialog, Is.True);
        });
    }

    [Test]
    public void ShouldDistinguishWaitingAndUnavailableQueues()
    {
        var entry = Entry(1, "skill");
        var waiting = SkillPlanner.Plan(
            Request(
                Queue(entry),
                Vitals(health: 50),
                Skillbook(
                    Skill(
                        "skill",
                        healthCondition: new HealthCondition(
                            minimumPercentExclusive: 90)))));
        var unavailable = SkillPlanner.Plan(
            Request(
                Queue(Entry(2, "missing")),
                Vitals(),
                SkillbookSnapshot.Empty));

        Assert.Multiple(() =>
        {
            Assert.That(waiting.Status, Is.EqualTo(SkillPlanStatus.Waiting));
            Assert.That(
                unavailable.Status,
                Is.EqualTo(SkillPlanStatus.Unavailable));
        });
    }

    [Test]
    public void ShouldRequireVitalsForHealthEvenWhenManaCheckIsDisabled()
    {
        var entry = Entry(1, "skill");
        var plan = SkillPlanner.Plan(
            Request(
                Queue(entry),
                vitals: null,
                Skillbook(
                    Skill(
                        "skill",
                        healthCondition: new HealthCondition(
                            maximumPercentInclusive: 2))),
                policy: new SkillUsePolicy(requireMana: false)));

        Assert.That(
            plan.Status,
            Is.EqualTo(SkillPlanStatus.SnapshotUnavailable));
    }

    private static SkillPlanningRequest Request(
        SkillQueueState queue,
        VitalsSnapshot? vitals,
        SkillbookSnapshot? skillbook,
        SkillCooldownState? cooldowns = null,
        MacroTimestamp? currentTime = null,
        SkillUsePolicy? policy = null) =>
        new(
            queue,
            vitals,
            skillbook,
            cooldowns,
            currentTime ?? MacroTimestamp.Zero,
            policy);

    private static SkillQueueState Queue(
        params SkillQueueEntry[] entries)
    {
        var queue = SkillQueueState.Empty;
        foreach (var entry in entries)
        {
            queue = queue.Add(entry);
        }

        return queue;
    }

    private static SkillQueueEntry Entry(long id, string name) =>
        new(new SkillQueueEntryId(id), name);

    private static VitalsSnapshot Vitals(
        int health = 100,
        int mana = 100) =>
        new(
            currentHealth: health,
            maximumHealth: 100,
            currentMana: mana,
            maximumMana: 100);

    private static SkillbookSnapshot Skillbook(
        params SkillSnapshot[] skills) =>
        new(skills);

    private static SkillSnapshot Skill(
        string name,
        int slot = 1,
        int manaCost = 0,
        bool isAssail = false,
        bool opensDialog = false,
        bool requiresDisarm = false,
        HealthCondition? healthCondition = null,
        bool isActionDelayed = false) =>
        new(
            name,
            slot,
            currentLevel: 0,
            maximumLevel: 100,
            manaCost,
            cooldown: TimeSpan.Zero,
            isAssail,
            opensDialog,
            requiresDisarm,
            healthCondition,
            isActionDelayed);
}
