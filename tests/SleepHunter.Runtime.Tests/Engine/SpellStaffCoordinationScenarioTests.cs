using SleepHunter.Runtime.Automation.Panels;
using SleepHunter.Runtime.Automation.Spells;
using SleepHunter.Runtime.Automation.Staves;
using SleepHunter.Runtime.Characters;
using SleepHunter.Runtime.Commands;
using SleepHunter.Runtime.Engine;
using SleepHunter.Runtime.Intents;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Tests.Scenarios;

namespace SleepHunter.Runtime.Tests.Engine;

public sealed class SpellStaffCoordinationScenarioTests
{
    private static readonly SpellCastTimingPolicy TestTiming = new(
        TimeSpan.FromMilliseconds(5),
        TimeSpan.FromMilliseconds(10),
        TimeSpan.FromMilliseconds(10),
        TimeSpan.FromMilliseconds(1));

    private static readonly PanelTransitionPolicy TestPanelPolicy = new(
        TimeSpan.FromMilliseconds(50),
        maximumAttempts: 2);

    private static readonly StaffEquipmentPolicy TestStaffPolicy = new(
        TimeSpan.FromMilliseconds(50),
        maximumAttempts: 2);

    private static readonly SpellExecutionPolicy TestPolicy = new(
        new SpellCastPolicy(requireMana: true, TestTiming),
        TestPanelPolicy,
        allowStaffSwitching: true,
        TestStaffPolicy);

    [Test]
    public void ShouldEquipStaffThenSwitchPanelAndCast()
    {
        var entry = Entry();
        var spell = Spell();
        var staff = Candidate(
            "wizard staff",
            CharacterClass.Wizard,
            castLines: 1);
        var catalog = Catalog(entry, staff);
        var scenario = CreateRunningScenario(
            ClientPanel.Stats,
            entry,
            spell,
            CharacterClass.Wizard,
            [new InventoryItemSnapshot(7, staff.Name)],
            equippedWeapon: null);

        var inventoryRequested = scenario.Send(
            new CastNextSpellCommand(TestPolicy, catalog));
        scenario.AdvanceBy(TimeSpan.FromTicks(1));
        var equipRequested = scenario.Observe(
            sequence: 2,
            activePanel: ClientPanel.Inventory,
            character: Character(CharacterClass.Wizard),
            inventory: new InventorySnapshot(
            [
                new InventoryItemSnapshot(7, staff.Name)
            ]),
            equipment: new EquipmentSnapshot(weaponName: null),
            vitals: Vitals(),
            spellbook: Spellbook(spell));
        scenario.AdvanceBy(TimeSpan.FromTicks(1));
        var spellPanelRequested = scenario.Observe(
            sequence: 3,
            activePanel: ClientPanel.Inventory,
            character: Character(CharacterClass.Wizard),
            inventory: InventorySnapshot.Empty,
            equipment: new EquipmentSnapshot(staff.Name),
            vitals: Vitals(),
            spellbook: Spellbook(spell));
        scenario.AdvanceBy(TimeSpan.FromTicks(1));
        var castRequested = scenario.Observe(
            sequence: 4,
            activePanel: ClientPanel.TemuairSpells,
            character: Character(CharacterClass.Wizard),
            inventory: InventorySnapshot.Empty,
            equipment: new EquipmentSnapshot(staff.Name),
            vitals: Vitals(),
            spellbook: Spellbook(spell));

        Assert.Multiple(() =>
        {
            Assert.That(
                inventoryRequested.Intent,
                Is.TypeOf<SwitchPanelIntent>());
            Assert.That(
                ((SwitchPanelIntent)inventoryRequested.Intent!).ActionId.Value,
                Is.EqualTo(1));
            Assert.That(
                equipRequested.Intent,
                Is.TypeOf<EquipWeaponIntent>());
            Assert.That(
                ((EquipWeaponIntent)equipRequested.Intent!).ActionId.Value,
                Is.EqualTo(2));
            Assert.That(
                spellPanelRequested.Intent,
                Is.TypeOf<SwitchPanelIntent>());
            Assert.That(
                ((SwitchPanelIntent)spellPanelRequested.Intent!).ActionId.Value,
                Is.EqualTo(3));
            Assert.That(castRequested.Intent, Is.TypeOf<CastSpellIntent>());
            Assert.That(
                ((CastSpellIntent)castRequested.Intent!).ActionId.Value,
                Is.EqualTo(4));
            Assert.That(
                castRequested.State.StaffSwitch?.Status,
                Is.EqualTo(StaffSwitchStatus.Succeeded));
            Assert.That(
                castRequested.State.SpellCast?.Status,
                Is.EqualTo(SpellCastStatus.Casting));
            Assert.That(castRequested.State.SpellCast?.CastLines, Is.EqualTo(1));
            Assert.That(
                castRequested.State.SpellCast?.CastDuration,
                Is.EqualTo(TimeSpan.FromMilliseconds(11)));
        });
    }

    [Test]
    public void ShouldIgnoreForeignClassStaffAndCastWithBaseLines()
    {
        var entry = Entry();
        var spell = Spell();
        var foreignStaff = Candidate(
            "priest staff",
            CharacterClass.Priest,
            castLines: 0);
        var scenario = CreateRunningScenario(
            ClientPanel.TemuairSpells,
            entry,
            spell,
            CharacterClass.Wizard,
            [new InventoryItemSnapshot(7, foreignStaff.Name)],
            equippedWeapon: null);

        var decision = scenario.Send(
            new CastNextSpellCommand(
                TestPolicy,
                Catalog(entry, foreignStaff)));

        Assert.Multiple(() =>
        {
            Assert.That(decision.Intent, Is.TypeOf<CastSpellIntent>());
            Assert.That(
                decision.State.StaffSwitch?.Status,
                Is.EqualTo(StaffSwitchStatus.NoChange));
            Assert.That(
                decision.State.StaffSwitch?.Selection?.Reason,
                Is.EqualTo(StaffSelectionReason.NoEligibleStaff));
            Assert.That(decision.State.SpellCast?.CastLines, Is.EqualTo(4));
            Assert.That(
                decision.State.SpellCast?.CastDuration,
                Is.EqualTo(TimeSpan.FromMilliseconds(41)));
        });
    }

    [Test]
    public void ShouldIgnoreStaffCatalogWhenSwitchingIsDisabled()
    {
        var entry = Entry();
        var spell = Spell();
        var staff = Candidate(
            "staff",
            CharacterClass.Wizard,
            castLines: 0);
        var policy = new SpellExecutionPolicy(
            new SpellCastPolicy(requireMana: true, TestTiming),
            TestPanelPolicy,
            allowStaffSwitching: false,
            TestStaffPolicy);
        var scenario = new MacroScenario();
        scenario.Send(new AddSpellQueueEntryCommand(entry));
        scenario.Observe(
            sequence: 1,
            activePanel: ClientPanel.TemuairSpells,
            vitals: Vitals(),
            spellbook: Spellbook(spell));
        scenario.Start();

        var decision = scenario.Send(
            new CastNextSpellCommand(policy, Catalog(entry, staff)));

        Assert.Multiple(() =>
        {
            Assert.That(decision.Intent, Is.TypeOf<CastSpellIntent>());
            Assert.That(decision.State.StaffSwitch, Is.Null);
            Assert.That(decision.State.SpellCast?.CastLines, Is.EqualTo(4));
        });
    }

    [Test]
    public void ShouldPropagateStaffPanelFailureWithoutAdvancingQueue()
    {
        var entry = Entry();
        var spell = Spell();
        var staff = Candidate(
            "staff",
            CharacterClass.Wizard,
            castLines: 1);
        var scenario = CreateRunningScenario(
            ClientPanel.Stats,
            entry,
            spell,
            CharacterClass.Wizard,
            [new InventoryItemSnapshot(7, staff.Name)],
            equippedWeapon: null);
        var requested = scenario.Send(
            new CastNextSpellCommand(TestPolicy, Catalog(entry, staff)));

        scenario.AdvanceBy(TestStaffPolicy.AttemptTimeout);
        var retry = scenario.Dispatch(requested.ScheduledEvents.Single().Input);
        scenario.AdvanceBy(TestStaffPolicy.AttemptTimeout);
        var failed = scenario.Dispatch(retry.ScheduledEvents.Single().Input);

        Assert.Multiple(() =>
        {
            Assert.That(failed.State.PendingAction, Is.Null);
            Assert.That(
                failed.State.StaffSwitch?.Status,
                Is.EqualTo(StaffSwitchStatus.PanelUnavailable));
            Assert.That(
                failed.State.SpellCast?.Status,
                Is.EqualTo(SpellCastStatus.StaffUnavailable));
            Assert.That(failed.State.SpellQueue.Cursor, Is.Zero);
        });
    }

    [Test]
    public void ShouldPropagateStaffEquipmentTimeout()
    {
        var entry = Entry();
        var spell = Spell();
        var staff = Candidate(
            "staff",
            CharacterClass.Wizard,
            castLines: 1);
        var scenario = CreateRunningScenario(
            ClientPanel.Inventory,
            entry,
            spell,
            CharacterClass.Wizard,
            [new InventoryItemSnapshot(7, staff.Name)],
            equippedWeapon: null);
        var requested = scenario.Send(
            new CastNextSpellCommand(TestPolicy, Catalog(entry, staff)));

        scenario.AdvanceBy(TestStaffPolicy.AttemptTimeout);
        var retry = scenario.Dispatch(requested.ScheduledEvents.Single().Input);
        scenario.AdvanceBy(TestStaffPolicy.AttemptTimeout);
        var failed = scenario.Dispatch(retry.ScheduledEvents.Single().Input);

        Assert.Multiple(() =>
        {
            Assert.That(failed.State.PendingAction, Is.Null);
            Assert.That(
                failed.State.StaffSwitch?.Status,
                Is.EqualTo(StaffSwitchStatus.TimedOut));
            Assert.That(
                failed.State.SpellCast?.Status,
                Is.EqualTo(SpellCastStatus.StaffUnavailable));
        });
    }

    [Test]
    public void ShouldRevalidateSpellBeforeIssuingEquipmentIntent()
    {
        var entry = Entry();
        var spell = Spell();
        var staff = Candidate(
            "staff",
            CharacterClass.Wizard,
            castLines: 1);
        var scenario = CreateRunningScenario(
            ClientPanel.Stats,
            entry,
            spell,
            CharacterClass.Wizard,
            [new InventoryItemSnapshot(7, staff.Name)],
            equippedWeapon: null);
        scenario.Send(
            new CastNextSpellCommand(TestPolicy, Catalog(entry, staff)));
        scenario.Send(new RemoveSpellQueueEntryCommand(entry.Id));
        scenario.AdvanceBy(TimeSpan.FromTicks(1));

        var confirmed = scenario.Observe(
            sequence: 2,
            activePanel: ClientPanel.Inventory,
            character: Character(CharacterClass.Wizard),
            inventory: new InventorySnapshot(
            [
                new InventoryItemSnapshot(7, staff.Name)
            ]),
            equipment: new EquipmentSnapshot(weaponName: null),
            vitals: Vitals(),
            spellbook: Spellbook(spell));

        Assert.Multiple(() =>
        {
            Assert.That(confirmed.Intent, Is.Null);
            Assert.That(confirmed.State.PendingAction, Is.Null);
            Assert.That(
                confirmed.State.SpellCast?.Status,
                Is.EqualTo(SpellCastStatus.QueueEmpty));
            Assert.That(
                confirmed.State.StaffSwitch?.Status,
                Is.EqualTo(StaffSwitchStatus.SelectionInvalidated));
        });
    }

    [Test]
    public void ShouldRevalidateSpellAfterEquipmentConfirmation()
    {
        var entry = Entry();
        var spell = Spell();
        var staff = Candidate(
            "staff",
            CharacterClass.Wizard,
            castLines: 1);
        var scenario = CreateRunningScenario(
            ClientPanel.Inventory,
            entry,
            spell,
            CharacterClass.Wizard,
            [new InventoryItemSnapshot(7, staff.Name)],
            equippedWeapon: null);
        scenario.Send(
            new CastNextSpellCommand(TestPolicy, Catalog(entry, staff)));
        scenario.AdvanceBy(TimeSpan.FromTicks(1));

        var confirmed = scenario.Observe(
            sequence: 2,
            activePanel: ClientPanel.Inventory,
            character: Character(CharacterClass.Wizard),
            inventory: InventorySnapshot.Empty,
            equipment: new EquipmentSnapshot(staff.Name),
            vitals: Vitals(),
            spellbook: Spellbook(Spell(castLines: 5)));

        Assert.Multiple(() =>
        {
            Assert.That(confirmed.Intent, Is.Null);
            Assert.That(confirmed.State.PendingAction, Is.Null);
            Assert.That(
                confirmed.State.StaffSwitch?.Status,
                Is.EqualTo(StaffSwitchStatus.Succeeded));
            Assert.That(
                confirmed.State.SpellCast?.Status,
                Is.EqualTo(SpellCastStatus.SelectionInvalidated));
        });
    }

    [Test]
    public void ShouldCancelSpellAndStaffWorkflowsWhenPaused()
    {
        var entry = Entry();
        var spell = Spell();
        var staff = Candidate(
            "staff",
            CharacterClass.Wizard,
            castLines: 1);
        var scenario = CreateRunningScenario(
            ClientPanel.Inventory,
            entry,
            spell,
            CharacterClass.Wizard,
            [new InventoryItemSnapshot(7, staff.Name)],
            equippedWeapon: null);
        scenario.Send(
            new CastNextSpellCommand(TestPolicy, Catalog(entry, staff)));

        var paused = scenario.Pause();

        Assert.Multiple(() =>
        {
            Assert.That(paused.State.PendingAction, Is.Null);
            Assert.That(
                paused.State.StaffSwitch?.Status,
                Is.EqualTo(StaffSwitchStatus.Cancelled));
            Assert.That(
                paused.State.SpellCast?.Status,
                Is.EqualTo(SpellCastStatus.Cancelled));
        });
    }

    private static MacroScenario CreateRunningScenario(
        ClientPanel panel,
        SpellQueueEntry entry,
        SpellSnapshot spell,
        CharacterClass characterClass,
        IEnumerable<InventoryItemSnapshot> inventory,
        string? equippedWeapon)
    {
        var scenario = new MacroScenario();
        scenario.Send(new AddSpellQueueEntryCommand(entry));
        scenario.Observe(
            sequence: 1,
            activePanel: panel,
            character: Character(characterClass),
            inventory: new InventorySnapshot(inventory),
            equipment: new EquipmentSnapshot(equippedWeapon),
            vitals: Vitals(),
            spellbook: Spellbook(spell));
        scenario.Start();
        return scenario;
    }

    private static SpellQueueEntry Entry() =>
        new(new SpellQueueEntryId(1), "spell");

    private static SpellSnapshot Spell(int castLines = 4) =>
        new(
            "spell",
            slot: 1,
            currentLevel: 0,
            maximumLevel: 100,
            castLines,
            manaCost: 10,
            cooldown: TimeSpan.Zero);

    private static StaffCandidate Candidate(
        string name,
        CharacterClass requiredClass,
        int castLines) =>
        new(
            name,
            requiredClass,
            requiredLevel: 0,
            requiredAbilityLevel: 0,
            castLines);

    private static SpellStaffCatalog Catalog(
        SpellQueueEntry entry,
        params StaffCandidate[] candidates) =>
        new(
        [
            new SpellStaffCandidateSet(entry.Id, candidates)
        ]);

    private static CharacterSnapshot Character(
        CharacterClass characterClass) =>
        new(characterClass, level: 99, abilityLevel: 99);

    private static VitalsSnapshot Vitals() =>
        new(100, 100, 100, 100);

    private static SpellbookSnapshot Spellbook(SpellSnapshot spell) =>
        new([spell]);
}
