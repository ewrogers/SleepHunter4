using SleepHunter.Runtime.Automation;
using SleepHunter.Runtime.Automation.Equipment;
using SleepHunter.Runtime.Automation.Panels;
using SleepHunter.Runtime.Automation.Skills;
using SleepHunter.Runtime.Commands;
using SleepHunter.Runtime.Engine;
using SleepHunter.Runtime.Intents;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Tests.Scenarios;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Tests.Engine;

public sealed class SkillUseScenarioTests
{
    private static readonly PanelTransitionPolicy TestPanelPolicy = new(
        TimeSpan.FromMilliseconds(50),
        maximumAttempts: 2);

    private static readonly DisarmPolicy TestDisarmPolicy = new(
        TimeSpan.FromMilliseconds(50),
        maximumAttempts: 2);

    private static readonly SkillExecutionPolicy TestPolicy = new(
        SkillUsePolicy.Default,
        TestPanelPolicy,
        TestDisarmPolicy,
        TimeSpan.FromMilliseconds(10));

    [Test]
    public void ShouldUseSkillAndRecordCooldownAtCompletion()
    {
        var entry = Entry(1, "skill");
        var skill = Skill("skill", cooldown: TimeSpan.FromSeconds(1));
        var scenario = CreateRunningScenario(
            ClientPanel.TemuairSkills,
            entry,
            skill);

        var requested = scenario.Send(new UseNextSkillCommand(TestPolicy));
        scenario.AdvanceBy(TestPolicy.ActionDuration);
        var completed = scenario.Dispatch(
            requested.ScheduledEvents.Single().Input);
        var stale = scenario.Send(new UseNextSkillCommand(TestPolicy));
        scenario.AdvanceBy(TimeSpan.FromTicks(1));
        scenario.Observe(
            sequence: 2,
            activePanel: ClientPanel.TemuairSkills,
            vitals: Vitals(),
            skillbook: Skillbook(skill));
        var cooling = scenario.Send(new UseNextSkillCommand(TestPolicy));

        Assert.Multiple(() =>
        {
            Assert.That(requested.Intent, Is.TypeOf<UseSkillIntent>());
            Assert.That(
                ((UseSkillIntent)requested.Intent!).ActionId.Value,
                Is.EqualTo(1));
            Assert.That(
                requested.State.SkillUse?.Status,
                Is.EqualTo(SkillUseStatus.Using));
            Assert.That(
                completed.State.SkillUse?.Status,
                Is.EqualTo(SkillUseStatus.Succeeded));
            Assert.That(
                stale.State.SkillUse?.Status,
                Is.EqualTo(SkillUseStatus.SnapshotUnavailable));
            Assert.That(
                completed.State.SkillCooldowns.GetReadyAt(
                    skill.Name,
                    MacroTimestamp.Zero),
                Is.EqualTo(
                    new MacroTimestamp(TimeSpan.FromMilliseconds(1010))));
            Assert.That(cooling.Intent, Is.Null);
            Assert.That(
                cooling.State.SkillUse?.Status,
                Is.EqualTo(SkillUseStatus.CoolingDown));
        });
    }

    [Test]
    public void ShouldDisarmAgainWhenRearmedDuringPanelTransition()
    {
        var entry = Entry(1, "skill");
        var skill = Skill("skill", requiresDisarm: true);
        var scenario = CreateRunningScenario(
            ClientPanel.Stats,
            entry,
            skill,
            equipment: new EquipmentSnapshot("weapon"));
        scenario.Send(new UseNextSkillCommand(TestPolicy));
        scenario.AdvanceBy(TimeSpan.FromTicks(1));
        scenario.Observe(
            sequence: 2,
            activePanel: ClientPanel.Stats,
            equipment: new EquipmentSnapshot(weaponName: null),
            vitals: Vitals(),
            skillbook: Skillbook(skill));
        scenario.AdvanceBy(TimeSpan.FromTicks(1));

        var rearmed = scenario.Observe(
            sequence: 3,
            activePanel: ClientPanel.TemuairSkills,
            equipment: new EquipmentSnapshot("new weapon"),
            vitals: Vitals(),
            skillbook: Skillbook(skill));

        Assert.Multiple(() =>
        {
            Assert.That(rearmed.Intent, Is.TypeOf<DisarmIntent>());
            Assert.That(
                ((DisarmIntent)rearmed.Intent!).ActionId.Value,
                Is.EqualTo(3));
            Assert.That(
                rearmed.State.SkillUse?.Status,
                Is.EqualTo(SkillUseStatus.WaitingForDisarm));
            Assert.That(
                rearmed.State.Disarm?.Status,
                Is.EqualTo(DisarmStatus.Disarming));
        });
    }

    [Test]
    public void ShouldAdvanceQueueOnlyWhenSkillIntentIsIssued()
    {
        var first = Entry(1, "first");
        var second = Entry(2, "second");
        var firstSkill = Skill("first", slot: 1);
        var secondSkill = Skill("second", slot: 2);
        var scenario = new MacroScenario();
        scenario.Send(new AddSkillQueueEntryCommand(first));
        scenario.Send(new AddSkillQueueEntryCommand(second));
        scenario.Observe(
            sequence: 1,
            activePanel: ClientPanel.Stats,
            vitals: Vitals(),
            skillbook: Skillbook(firstSkill, secondSkill));
        scenario.Start();

        scenario.Send(new UseNextSkillCommand(TestPolicy));
        var waitingCursor = scenario.State.SkillQueue.Cursor;
        scenario.AdvanceBy(TimeSpan.FromTicks(1));
        var confirmed = scenario.Observe(
            sequence: 2,
            activePanel: ClientPanel.TemuairSkills,
            vitals: Vitals(),
            skillbook: Skillbook(firstSkill, secondSkill));

        Assert.Multiple(() =>
        {
            Assert.That(waitingCursor, Is.Zero);
            Assert.That(confirmed.Intent, Is.TypeOf<UseSkillIntent>());
            Assert.That(confirmed.State.SkillQueue.Cursor, Is.EqualTo(1));
            Assert.That(
                confirmed.State.SkillUse?.Status,
                Is.EqualTo(SkillUseStatus.Using));
        });
    }

    [Test]
    public void ShouldDisarmThenSwitchPanelAndUseSkill()
    {
        var entry = Entry(1, "skill");
        var skill = Skill("skill", requiresDisarm: true);
        var scenario = CreateRunningScenario(
            ClientPanel.Stats,
            entry,
            skill,
            equipment: new EquipmentSnapshot("weapon", "shield"));

        var disarmRequested = scenario.Send(
            new UseNextSkillCommand(TestPolicy));
        scenario.AdvanceBy(TimeSpan.FromTicks(1));
        var panelRequested = scenario.Observe(
            sequence: 2,
            activePanel: ClientPanel.Stats,
            equipment: new EquipmentSnapshot(
                weaponName: null,
                shieldName: null),
            vitals: Vitals(),
            skillbook: Skillbook(skill));
        scenario.AdvanceBy(TimeSpan.FromTicks(1));
        var skillRequested = scenario.Observe(
            sequence: 3,
            activePanel: ClientPanel.TemuairSkills,
            equipment: new EquipmentSnapshot(
                weaponName: null,
                shieldName: null),
            vitals: Vitals(),
            skillbook: Skillbook(skill));

        Assert.Multiple(() =>
        {
            Assert.That(disarmRequested.Intent, Is.TypeOf<DisarmIntent>());
            Assert.That(
                ((DisarmIntent)disarmRequested.Intent!).ActionId.Value,
                Is.EqualTo(1));
            Assert.That(panelRequested.Intent, Is.TypeOf<SwitchPanelIntent>());
            Assert.That(
                ((SwitchPanelIntent)panelRequested.Intent!).ActionId.Value,
                Is.EqualTo(2));
            Assert.That(skillRequested.Intent, Is.TypeOf<UseSkillIntent>());
            Assert.That(
                ((UseSkillIntent)skillRequested.Intent!).ActionId.Value,
                Is.EqualTo(3));
            Assert.That(
                skillRequested.State.Disarm?.Status,
                Is.EqualTo(DisarmStatus.Succeeded));
        });
    }

    [Test]
    public void ShouldAssailWithoutOpeningSkillPanel()
    {
        var entry = Entry(1, "assail");
        var skill = Skill("assail", isAssail: true);
        var planning = new SkillUsePolicy(disarmForAssails: false);
        var policy = new SkillExecutionPolicy(
            planning,
            TestPanelPolicy,
            TestDisarmPolicy,
            TimeSpan.FromMilliseconds(10));
        var scenario = CreateRunningScenario(
            ClientPanel.Stats,
            entry,
            skill);

        var decision = scenario.Send(new UseNextSkillCommand(policy));

        Assert.Multiple(() =>
        {
            Assert.That(decision.Intent, Is.TypeOf<AssailIntent>());
            Assert.That(
                decision.State.SkillUse?.Status,
                Is.EqualTo(SkillUseStatus.Assailing));
            Assert.That(decision.State.PanelTransition, Is.Null);
        });
    }

    [Test]
    public void ShouldUsePanelForIndividualAssail()
    {
        var entry = Entry(1, "assail");
        var skill = Skill("assail", isAssail: true);
        var planning = new SkillUsePolicy(
            assailMode: AssailMode.SkillSlot,
            disarmForAssails: false);
        var policy = new SkillExecutionPolicy(
            planning,
            TestPanelPolicy,
            TestDisarmPolicy,
            TimeSpan.FromMilliseconds(10));
        var scenario = CreateRunningScenario(
            ClientPanel.Stats,
            entry,
            skill);

        var decision = scenario.Send(new UseNextSkillCommand(policy));

        Assert.Multiple(() =>
        {
            Assert.That(decision.Intent, Is.TypeOf<SwitchPanelIntent>());
            Assert.That(
                decision.State.SkillUse?.Status,
                Is.EqualTo(SkillUseStatus.WaitingForPanel));
        });
    }

    [Test]
    public void ShouldPropagateDisarmTimeoutWithoutAdvancingQueue()
    {
        var entry = Entry(1, "skill");
        var skill = Skill("skill", requiresDisarm: true);
        var scenario = CreateRunningScenario(
            ClientPanel.TemuairSkills,
            entry,
            skill,
            equipment: new EquipmentSnapshot("weapon"));
        var requested = scenario.Send(
            new UseNextSkillCommand(TestPolicy));

        scenario.AdvanceBy(TestDisarmPolicy.AttemptTimeout);
        var retry = scenario.Dispatch(
            requested.ScheduledEvents.Single().Input);
        scenario.AdvanceBy(TestDisarmPolicy.AttemptTimeout);
        var failed = scenario.Dispatch(
            retry.ScheduledEvents.Single().Input);

        Assert.Multiple(() =>
        {
            Assert.That(retry.Intent, Is.TypeOf<DisarmIntent>());
            Assert.That(failed.State.PendingAction, Is.Null);
            Assert.That(
                failed.State.Disarm?.Status,
                Is.EqualTo(DisarmStatus.TimedOut));
            Assert.That(
                failed.State.SkillUse?.Status,
                Is.EqualTo(SkillUseStatus.DisarmUnavailable));
            Assert.That(failed.State.SkillQueue.Cursor, Is.Zero);
        });
    }

    [Test]
    public void ShouldNotConfirmDisarmFromSnapshotCapturedAtIssueTime()
    {
        var entry = Entry(1, "skill");
        var skill = Skill("skill", requiresDisarm: true);
        var scenario = CreateRunningScenario(
            ClientPanel.TemuairSkills,
            entry,
            skill,
            equipment: new EquipmentSnapshot("weapon"));
        scenario.Send(new UseNextSkillCommand(TestPolicy));

        var stale = scenario.Observe(
            sequence: 2,
            activePanel: ClientPanel.TemuairSkills,
            captureStartedAt: MacroTimestamp.Zero,
            equipment: new EquipmentSnapshot(weaponName: null),
            vitals: Vitals(),
            skillbook: Skillbook(skill));

        Assert.Multiple(() =>
        {
            Assert.That(stale.Intent, Is.Null);
            Assert.That(stale.State.PendingAction, Is.Not.Null);
            Assert.That(
                stale.State.Disarm?.Status,
                Is.EqualTo(DisarmStatus.Disarming));
            Assert.That(
                stale.State.SkillUse?.Status,
                Is.EqualTo(SkillUseStatus.WaitingForDisarm));
        });
    }

    [Test]
    public void ShouldPropagatePanelTimeoutWithoutAdvancingQueue()
    {
        var first = Entry(1, "first");
        var second = Entry(2, "second");
        var scenario = new MacroScenario();
        scenario.Send(new AddSkillQueueEntryCommand(first));
        scenario.Send(new AddSkillQueueEntryCommand(second));
        scenario.Observe(
            sequence: 1,
            activePanel: ClientPanel.Stats,
            vitals: Vitals(),
            skillbook: Skillbook(
                Skill("first", slot: 1),
                Skill("second", slot: 2)));
        scenario.Start();
        var requested = scenario.Send(
            new UseNextSkillCommand(TestPolicy));

        scenario.AdvanceBy(TestPanelPolicy.AttemptTimeout);
        var retry = scenario.Dispatch(
            requested.ScheduledEvents.Single().Input);
        scenario.AdvanceBy(TestPanelPolicy.AttemptTimeout);
        var failed = scenario.Dispatch(
            retry.ScheduledEvents.Single().Input);

        Assert.Multiple(() =>
        {
            Assert.That(failed.State.PendingAction, Is.Null);
            Assert.That(
                failed.State.PanelTransition?.Status,
                Is.EqualTo(PanelTransitionStatus.TimedOut));
            Assert.That(
                failed.State.SkillUse?.Status,
                Is.EqualTo(SkillUseStatus.PanelUnavailable));
            Assert.That(failed.State.SkillQueue.Cursor, Is.Zero);
        });
    }

    [Test]
    public void ShouldRevalidateHealthAfterPanelConfirmation()
    {
        var entry = Entry(1, "skill");
        var skill = Skill(
            "skill",
            healthCondition: new HealthCondition(
                minimumPercentExclusive: 90));
        var scenario = CreateRunningScenario(
            ClientPanel.Stats,
            entry,
            skill,
            health: 91);
        scenario.Send(new UseNextSkillCommand(TestPolicy));
        scenario.AdvanceBy(TimeSpan.FromTicks(1));

        var confirmed = scenario.Observe(
            sequence: 2,
            activePanel: ClientPanel.TemuairSkills,
            vitals: Vitals(health: 90),
            skillbook: Skillbook(skill));

        Assert.Multiple(() =>
        {
            Assert.That(confirmed.Intent, Is.Null);
            Assert.That(confirmed.State.PendingAction, Is.Null);
            Assert.That(
                confirmed.State.SkillUse?.Status,
                Is.EqualTo(SkillUseStatus.WaitingForHealth));
        });
    }

    [Test]
    public void ShouldInvalidateRemovedSelectionAfterDisarm()
    {
        var entry = Entry(1, "skill");
        var skill = Skill("skill", requiresDisarm: true);
        var scenario = CreateRunningScenario(
            ClientPanel.TemuairSkills,
            entry,
            skill,
            equipment: new EquipmentSnapshot("weapon"));
        scenario.Send(new UseNextSkillCommand(TestPolicy));
        scenario.Send(new RemoveSkillQueueEntryCommand(entry.Id));
        scenario.AdvanceBy(TimeSpan.FromTicks(1));

        var confirmed = scenario.Observe(
            sequence: 2,
            activePanel: ClientPanel.TemuairSkills,
            equipment: new EquipmentSnapshot(weaponName: null),
            vitals: Vitals(),
            skillbook: Skillbook(skill));

        Assert.Multiple(() =>
        {
            Assert.That(confirmed.Intent, Is.Null);
            Assert.That(confirmed.State.PendingAction, Is.Null);
            Assert.That(
                confirmed.State.Disarm?.Status,
                Is.EqualTo(DisarmStatus.Succeeded));
            Assert.That(
                confirmed.State.SkillUse?.Status,
                Is.EqualTo(SkillUseStatus.QueueEmpty));
        });
    }

    [Test]
    public void ShouldCancelSkillAndDisarmWhenPaused()
    {
        var entry = Entry(1, "skill");
        var skill = Skill("skill", requiresDisarm: true);
        var scenario = CreateRunningScenario(
            ClientPanel.TemuairSkills,
            entry,
            skill,
            equipment: new EquipmentSnapshot("weapon"));
        scenario.Send(new UseNextSkillCommand(TestPolicy));

        var paused = scenario.Pause();

        Assert.Multiple(() =>
        {
            Assert.That(paused.State.PendingAction, Is.Null);
            Assert.That(
                paused.State.SkillUse?.Status,
                Is.EqualTo(SkillUseStatus.Cancelled));
            Assert.That(
                paused.State.Disarm?.Status,
                Is.EqualTo(DisarmStatus.Cancelled));
        });
    }

    [Test]
    public void ShouldRequireEquipmentSnapshotOnlyForDisarm()
    {
        var entry = Entry(1, "skill");
        var skill = Skill("skill", requiresDisarm: true);
        var scenario = CreateRunningScenario(
            ClientPanel.TemuairSkills,
            entry,
            skill);

        var decision = scenario.Send(new UseNextSkillCommand(TestPolicy));

        Assert.Multiple(() =>
        {
            Assert.That(decision.Intent, Is.Null);
            Assert.That(
                decision.State.SkillUse?.Status,
                Is.EqualTo(SkillUseStatus.SnapshotUnavailable));
            Assert.That(
                decision.State.Disarm?.Status,
                Is.EqualTo(DisarmStatus.SnapshotUnavailable));
        });
    }

    [Test]
    public void ShouldApplySkillQueueCommandsByStableIdentifier()
    {
        var first = Entry(1, "first");
        var second = Entry(2, "second");
        var scenario = new MacroScenario();

        scenario.Send(new AddSkillQueueEntryCommand(first));
        scenario.Send(new AddSkillQueueEntryCommand(second));
        scenario.Send(new MoveSkillQueueEntryCommand(second.Id, 0));
        scenario.Send(
            new UpdateSkillQueueEntryCommand(
                new SkillQueueEntry(first.Id, "renamed")));
        scenario.Send(new RemoveSkillQueueEntryCommand(second.Id));
        var cleared = scenario.Send(new ClearSkillQueueCommand());

        Assert.That(cleared.State.SkillQueue.Entries, Is.Empty);
    }

    private static MacroScenario CreateRunningScenario(
        ClientPanel panel,
        SkillQueueEntry entry,
        SkillSnapshot skill,
        EquipmentSnapshot? equipment = null,
        int health = 100)
    {
        var scenario = new MacroScenario();
        scenario.Send(new AddSkillQueueEntryCommand(entry));
        scenario.Observe(
            sequence: 1,
            activePanel: panel,
            equipment: equipment,
            vitals: Vitals(health),
            skillbook: Skillbook(skill));
        scenario.Start();
        return scenario;
    }

    private static SkillQueueEntry Entry(
        long id,
        string name) =>
        new(new SkillQueueEntryId(id), name);

    private static SkillSnapshot Skill(
        string name,
        int slot = 1,
        int manaCost = 0,
        TimeSpan? cooldown = null,
        bool isAssail = false,
        bool requiresDisarm = false,
        HealthCondition? healthCondition = null) =>
        new(
            name,
            slot,
            currentLevel: 0,
            maximumLevel: 100,
            manaCost,
            cooldown ?? TimeSpan.Zero,
            isAssail,
            opensDialog: false,
            requiresDisarm,
            healthCondition);

    private static VitalsSnapshot Vitals(
        int health = 100,
        int mana = 100) =>
        new(
            health,
            maximumHealth: 100,
            mana,
            maximumMana: 100);

    private static SkillbookSnapshot Skillbook(
        params SkillSnapshot[] skills) =>
        new(skills);
}
