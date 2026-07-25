using SleepHunter.Runtime.Automation.Equipment;
using SleepHunter.Runtime.Automation.Flowering;
using SleepHunter.Runtime.Automation.Panels;
using SleepHunter.Runtime.Automation.Spells;
using SleepHunter.Runtime.Automation.Staves;
using SleepHunter.Runtime.Characters;
using SleepHunter.Runtime.Commands;
using SleepHunter.Runtime.Engine;
using SleepHunter.Runtime.Events;
using SleepHunter.Runtime.Intents;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Tests.Scenarios;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Tests.Engine;

public sealed class FlowerExecutionScenarioTests
{
    private static readonly SpellCastTimingPolicy TestTiming = new(
        zeroLineDuration: TimeSpan.Zero,
        singleLineDuration: TimeSpan.FromMilliseconds(10),
        multiLineDurationPerLine: TimeSpan.FromMilliseconds(10),
        completionPadding: TimeSpan.FromMilliseconds(1));

    private static readonly SpellExecutionPolicy TestSpellPolicy = new(
        new SpellCastPolicy(requireMana: true, TestTiming),
        new PanelTransitionPolicy(
            TimeSpan.FromMilliseconds(50),
            maximumAttempts: 2),
        allowStaffSwitching: false);

    private static readonly FlowerExecutionPolicy TestPolicy = new(
        spell: TestSpellPolicy);

    [Test]
    public void ShouldCastPlantAndRecordQueueScheduleWhenIntentIsIssued()
    {
        var interval = TimeSpan.FromSeconds(1);
        var entry = new FlowerQueueEntry(
            new FlowerQueueEntryId(1),
            SpellTarget.Self,
            interval);
        var plant = Spell(
            FlowerSpellNames.Plant,
            slot: 1,
            manaCost: 100,
            cooldown: TimeSpan.FromSeconds(1));
        var scenario = CreateRunningScenario(
            [plant],
            currentMana: 500);
        scenario.Send(new AddFlowerQueueEntryCommand(entry));
        scenario.AdvanceBy(interval);

        var issuedAt = scenario.CurrentTime;
        var requested = scenario.Send(new FlowerCommand(TestPolicy));
        var intent = (CastSpellIntent)requested.Intent!;
        scenario.AdvanceBy(TestTiming.CalculateDuration(plant.CastLines));
        var completed = scenario.Dispatch(
            requested.ScheduledEvents.Single().Input);

        Assert.Multiple(() =>
        {
            Assert.That(intent.SpellName, Is.EqualTo(plant.Name));
            Assert.That(intent.Target, Is.EqualTo(SpellTarget.Self));
            Assert.That(
                requested.State.Flower?.Status,
                Is.EqualTo(FlowerStatus.Casting));
            Assert.That(
                requested.State.Flower?.Action,
                Is.EqualTo(FlowerActionKind.Plant));
            Assert.That(
                requested.State.FlowerSchedules.GetReadyAt(entry.Id),
                Is.EqualTo(
                    issuedAt.Add(interval)));
            Assert.That(
                completed.State.Flower?.Status,
                Is.EqualTo(FlowerStatus.Succeeded));
            Assert.That(
                completed.State.Flower?.FloweredAt,
                Is.EqualTo(completed.State.SpellCast?.CompletesAt));
            Assert.That(
                completed.State.SpellCooldowns.GetReadyAt(
                    plant.Name,
                    scenario.CurrentTime),
                Is.EqualTo(
                    scenario.CurrentTime.Add(plant.Cooldown)));
        });
    }

    [Test]
    public void ShouldRotateConfiguredFlowerAreaOnlyWhenPlantIsIssued()
    {
        var area = SpellTarget.RelativeArea(0, 0, 0, 1);
        var entry = Entry(1, area);
        var plant = Spell(FlowerSpellNames.Plant, slot: 1);
        var scenario = CreateRunningScenario([plant]);
        scenario.Send(new AddFlowerQueueEntryCommand(entry));

        var first = scenario.Send(new FlowerCommand(TestPolicy));
        scenario.AdvanceBy(first.State.SpellCast!.CastDuration!.Value);
        scenario.Dispatch(first.ScheduledEvents.Single().Input);
        scenario.AdvanceBy(TimeSpan.FromTicks(1));
        scenario.Observe(
            sequence: 2,
            activePanel: ClientPanel.TemuairSpells,
            vitals: Vitals(),
            spellbook: new SpellbookSnapshot([plant]),
            location: SourceLocation());
        var second = scenario.Send(new FlowerCommand(TestPolicy));

        Assert.Multiple(() =>
        {
            Assert.That(
                ((CastSpellIntent)first.Intent!).Target,
                Is.EqualTo(SpellTarget.RelativeTile(0, 0)));
            Assert.That(
                ((CastSpellIntent)second.Intent!).Target,
                Is.EqualTo(SpellTarget.RelativeTile(0, -1)));
            Assert.That(
                second.State.FlowerTargetRotations.GetCursor(entry.Id.Value),
                Is.EqualTo(2));
            Assert.That(second.State.SpellTargetRotations.Count, Is.Zero);
        });
    }

    [Test]
    public void ShouldFlowerAutomaticWaitingCharacterBeforeVineyard()
    {
        var plant = Spell(FlowerSpellNames.Plant, slot: 1);
        var vineyard = Spell(FlowerSpellNames.Vineyard, slot: 2);
        var scenario = CreateRunningScenario([plant, vineyard]);
        scenario.ObserveClientRoster(
            sequence: 1,
            [
                Client(
                    "source",
                    location: SourceLocation(),
                    client: scenario.Client),
                Client(
                    "waiting",
                    isWaitingForMana: true,
                    location: NearbyLocation())
            ]);
        var policy = new FlowerExecutionPolicy(
            target: new FlowerTargetPolicy(
                autoFlowerWaitingCharacters: true,
                prioritizeAlternateCharacters: true),
            spell: TestSpellPolicy,
            useVineyard: true);

        var decision = scenario.Send(new FlowerCommand(policy));
        var intent = (CastSpellIntent)decision.Intent!;

        Assert.Multiple(() =>
        {
            Assert.That(intent.SpellName, Is.EqualTo(plant.Name));
            Assert.That(
                intent.Target,
                Is.EqualTo(SpellTarget.RelativeTile(5, 5)));
            Assert.That(
                decision.State.Flower?.Plan.SelectionKind,
                Is.EqualTo(FlowerSelectionKind.WaitingCharacter));
        });
    }

    [Test]
    public void ShouldDeferCharacterFlowerWhenRosterLacksSource()
    {
        var plant = Spell(FlowerSpellNames.Plant, slot: 1);
        var scenario = CreateRunningScenario([plant]);
        scenario.ObserveClientRoster(
            sequence: 1,
            [
                Client(
                    "waiting",
                    isWaitingForMana: true,
                    location: NearbyLocation())
            ]);
        var policy = new FlowerExecutionPolicy(
            target: new FlowerTargetPolicy(
                autoFlowerWaitingCharacters: true),
            spell: TestSpellPolicy);

        var decision = scenario.Send(new FlowerCommand(policy));

        Assert.Multiple(() =>
        {
            Assert.That(decision.Intent, Is.Null);
            Assert.That(decision.State.PendingAction, Is.Null);
            Assert.That(
                decision.State.Flower?.Status,
                Is.EqualTo(FlowerStatus.TargetUnavailable));
            Assert.That(
                decision.State.SpellCast?.Status,
                Is.EqualTo(SpellCastStatus.TargetUnavailable));
            Assert.That(
                decision.State.SpellCast?.TargetStatus,
                Is.EqualTo(TargetLocationStatus.SourceUnavailable));
        });
    }

    [Test]
    public void ShouldCastVineyardBeforeConfiguredTarget()
    {
        var entry = Entry(1, SpellTarget.Self);
        var plant = Spell(FlowerSpellNames.Plant, slot: 1);
        var vineyard = Spell(FlowerSpellNames.Vineyard, slot: 2);
        var scenario = CreateRunningScenario([plant, vineyard]);
        scenario.Send(new AddFlowerQueueEntryCommand(entry));
        var policy = new FlowerExecutionPolicy(
            spell: TestSpellPolicy,
            useVineyard: true);

        var decision = scenario.Send(new FlowerCommand(policy));
        var intent = (CastSpellIntent)decision.Intent!;

        Assert.Multiple(() =>
        {
            Assert.That(intent.SpellName, Is.EqualTo(vineyard.Name));
            Assert.That(intent.Target, Is.EqualTo(SpellTarget.None));
            Assert.That(
                decision.State.Flower?.Action,
                Is.EqualTo(FlowerActionKind.Vineyard));
            Assert.That(
                decision.State.FlowerQueue.Cursor,
                Is.Zero);
            Assert.That(
                decision.State.FlowerSchedules.GetReadyAt(entry.Id),
                Is.EqualTo(MacroTimestamp.Zero));
            Assert.That(decision.State.FlowerTargetRotations.GetCursor(1), Is.Zero);
        });
    }

    [Test]
    public void ShouldRestoreManaThenPlantFromFreshSnapshot()
    {
        var entry = Entry(1, SpellTarget.Self);
        var restoration = Spell(
            FlowerSpellNames.ManaRestoration,
            slot: 2);
        var plant = Spell(
            FlowerSpellNames.Plant,
            slot: 1,
            manaCost: 100);
        var scenario = CreateRunningScenario(
            [plant, restoration],
            currentMana: 50);
        scenario.Send(new AddFlowerQueueEntryCommand(entry));
        var policy = new FlowerExecutionPolicy(
            spell: TestSpellPolicy,
            restoreManaOnDemand: true);

        var restore = scenario.Send(new FlowerCommand(policy));
        scenario.AdvanceBy(
            TestTiming.CalculateDuration(restoration.CastLines));
        scenario.Dispatch(restore.ScheduledEvents.Single().Input);
        scenario.AdvanceBy(TimeSpan.FromTicks(1));
        scenario.Observe(
            sequence: 2,
            activePanel: ClientPanel.TemuairSpells,
            vitals: Vitals(currentMana: 500),
            spellbook: new SpellbookSnapshot([plant, restoration]),
            location: SourceLocation());
        var flower = scenario.Send(new FlowerCommand(policy));

        Assert.Multiple(() =>
        {
            Assert.That(
                ((CastSpellIntent)restore.Intent!).SpellName,
                Is.EqualTo(restoration.Name));
            Assert.That(
                restore.State.Flower?.Action,
                Is.EqualTo(FlowerActionKind.RestoreMana));
            Assert.That(
                ((CastSpellIntent)flower.Intent!).SpellName,
                Is.EqualTo(plant.Name));
            Assert.That(
                flower.State.Flower?.Action,
                Is.EqualTo(FlowerActionKind.Plant));
        });
    }

    [Test]
    public void ShouldInvalidateCharacterThatMovesAwayDuringPanelSwitch()
    {
        var entry = Entry(
            1,
            SpellTarget.Character("alt"));
        var plant = Spell(FlowerSpellNames.Plant, slot: 1);
        var scenario = CreateRunningScenario(
            [plant],
            activePanel: ClientPanel.Stats);
        scenario.Send(new AddFlowerQueueEntryCommand(entry));
        scenario.ObserveClientRoster(
            sequence: 1,
            [Client("alt", location: NearbyLocation())]);

        var panel = scenario.Send(new FlowerCommand(TestPolicy));
        scenario.ObserveClientRoster(
            sequence: 2,
            [
                Client(
                    "alt",
                    location: new MapLocationSnapshot(
                        2,
                        "other map",
                        50,
                        50))
            ]);
        scenario.AdvanceBy(TimeSpan.FromTicks(1));
        var confirmed = scenario.Observe(
            sequence: 2,
            activePanel: ClientPanel.TemuairSpells,
            vitals: Vitals(),
            spellbook: new SpellbookSnapshot([plant]),
            location: SourceLocation());

        Assert.Multiple(() =>
        {
            Assert.That(panel.Intent, Is.TypeOf<SwitchPanelIntent>());
            Assert.That(confirmed.Intent, Is.Null);
            Assert.That(confirmed.State.PendingAction, Is.Null);
            Assert.That(
                confirmed.State.Flower?.Status,
                Is.EqualTo(FlowerStatus.SelectionInvalidated));
            Assert.That(
                confirmed.State.SpellCast?.Status,
                Is.EqualTo(SpellCastStatus.SelectionInvalidated));
        });
    }

    [Test]
    public void ShouldCancelFlowerCastWhenPaused()
    {
        var scenario = CreateRunningScenario(
            [Spell(FlowerSpellNames.Plant, slot: 1)]);
        scenario.Send(
            new AddFlowerQueueEntryCommand(
                Entry(1, SpellTarget.Self)));
        scenario.Send(new FlowerCommand(TestPolicy));

        var paused = scenario.Pause();

        Assert.Multiple(() =>
        {
            Assert.That(paused.State.PendingAction, Is.Null);
            Assert.That(
                paused.State.Flower?.Status,
                Is.EqualTo(FlowerStatus.Cancelled));
            Assert.That(
                paused.State.SpellCast?.Status,
                Is.EqualTo(SpellCastStatus.Cancelled));
        });
    }

    [Test]
    public void ShouldReuseStaffAndPanelPrerequisitesForPlant()
    {
        var plant = Spell(FlowerSpellNames.Plant, slot: 1);
        var staff = new StaffCandidate(
            "flower staff",
            CharacterClass.Wizard,
            requiredLevel: 0,
            requiredAbilityLevel: 0,
            castLines: 0);
        var character = new CharacterSnapshot(
            CharacterClass.Wizard,
            level: 99,
            abilityLevel: 99);
        var inventory = new InventorySnapshot(
        [
            new InventoryItemSnapshot(7, staff.Name)
        ]);
        var spellPolicy = new SpellExecutionPolicy(
            new SpellCastPolicy(requireMana: true, TestTiming),
            new PanelTransitionPolicy(
                TimeSpan.FromMilliseconds(50),
                maximumAttempts: 2),
            allowStaffSwitching: true,
            new StaffEquipmentPolicy(
                TimeSpan.FromMilliseconds(50),
                maximumAttempts: 2));
        var policy = new FlowerExecutionPolicy(spell: spellPolicy);
        var catalog = new FlowerStaffCatalog(
        [
            new FlowerStaffCandidateSet(
                FlowerActionKind.Plant,
                [staff])
        ]);
        var scenario = new MacroScenario();
        scenario.Observe(
            sequence: 1,
            activePanel: ClientPanel.TemuairSpells,
            character: character,
            inventory: inventory,
            equipment: new EquipmentSnapshot(weaponName: null),
            vitals: Vitals(),
            spellbook: new SpellbookSnapshot([plant]),
            location: SourceLocation());
        scenario.Start();
        scenario.Send(
            new AddFlowerQueueEntryCommand(
                Entry(1, SpellTarget.Self)));

        var inventoryPanel = scenario.Send(
            new FlowerCommand(policy, catalog));
        scenario.AdvanceBy(TimeSpan.FromTicks(1));
        var equip = scenario.Observe(
            sequence: 2,
            activePanel: ClientPanel.Inventory,
            character: character,
            inventory: inventory,
            equipment: new EquipmentSnapshot(weaponName: null),
            vitals: Vitals(),
            spellbook: new SpellbookSnapshot([plant]),
            location: SourceLocation());
        scenario.AdvanceBy(TimeSpan.FromTicks(1));
        var spellPanel = scenario.Observe(
            sequence: 3,
            activePanel: ClientPanel.Inventory,
            character: character,
            inventory: InventorySnapshot.Empty,
            equipment: new EquipmentSnapshot(staff.Name),
            vitals: Vitals(),
            spellbook: new SpellbookSnapshot([plant]),
            location: SourceLocation());
        scenario.AdvanceBy(TimeSpan.FromTicks(1));
        var cast = scenario.Observe(
            sequence: 4,
            activePanel: ClientPanel.TemuairSpells,
            character: character,
            inventory: InventorySnapshot.Empty,
            equipment: new EquipmentSnapshot(staff.Name),
            vitals: Vitals(),
            spellbook: new SpellbookSnapshot([plant]),
            location: SourceLocation());

        Assert.Multiple(() =>
        {
            Assert.That(
                inventoryPanel.Intent,
                Is.TypeOf<SwitchPanelIntent>());
            Assert.That(equip.Intent, Is.TypeOf<EquipWeaponIntent>());
            Assert.That(spellPanel.Intent, Is.TypeOf<SwitchPanelIntent>());
            Assert.That(cast.Intent, Is.TypeOf<CastSpellIntent>());
            Assert.That(
                ((CastSpellIntent)cast.Intent!).SpellName,
                Is.EqualTo(plant.Name));
            Assert.That(
                cast.State.Flower?.Status,
                Is.EqualTo(FlowerStatus.Casting));
        });
    }

    [Test]
    public void ShouldIgnoreStaleAndFutureClientSets()
    {
        var scenario = new MacroScenario();
        var first = scenario.ObserveClientRoster(
            sequence: 1,
            [Client("first", location: NearbyLocation())]);
        var stale = scenario.ObserveClientRoster(
            sequence: 1,
            [Client("stale", location: NearbyLocation())]);
        var future = scenario.ObserveClientRoster(
            sequence: 2,
            [Client("future", location: NearbyLocation())],
            capturedAt: new MacroTimestamp(TimeSpan.FromTicks(1)));

        Assert.Multiple(() =>
        {
            Assert.That(
                first.State.ClientRoster.Sequence?.Value,
                Is.EqualTo(1));
            Assert.That(stale.State, Is.SameAs(first.State));
            Assert.That(future.State, Is.SameAs(first.State));
        });
    }

    [Test]
    public void ShouldApplyFlowerQueueCommandsAndSynchronizeSchedules()
    {
        var first = new FlowerQueueEntry(
            new FlowerQueueEntryId(1),
            SpellTarget.Self,
            interval: TimeSpan.FromSeconds(1));
        var second = new FlowerQueueEntry(
            new FlowerQueueEntryId(2),
            SpellTarget.RelativeTile(1, 0),
            interval: TimeSpan.FromSeconds(1));
        var scenario = new MacroScenario();
        scenario.Send(new AddFlowerQueueEntryCommand(first));
        scenario.Send(new AddFlowerQueueEntryCommand(second));
        scenario.Send(new MoveFlowerQueueEntryCommand(second.Id, 0));
        scenario.AdvanceBy(TimeSpan.FromMilliseconds(500));
        var updatedEntry = new FlowerQueueEntry(
            first.Id,
            first.Target,
            interval: TimeSpan.FromSeconds(2));
        var updated = scenario.Send(
            new UpdateFlowerQueueEntryCommand(updatedEntry));
        var removed = scenario.Send(
            new RemoveFlowerQueueEntryCommand(second.Id));
        var cleared = scenario.Send(new ClearFlowerQueueCommand());

        Assert.Multiple(() =>
        {
            Assert.That(
                updated.State.FlowerQueue.Entries.Select(entry => entry.Id),
                Is.EqualTo(new[] { second.Id, first.Id }));
            Assert.That(
                updated.State.FlowerSchedules.GetReadyAt(first.Id),
                Is.EqualTo(
                    scenario.CurrentTime.Add(TimeSpan.FromSeconds(2))));
            Assert.That(
                removed.State.FlowerSchedules.GetReadyAt(second.Id),
                Is.Null);
            Assert.That(cleared.State.FlowerQueue.Entries, Is.Empty);
            Assert.That(cleared.State.FlowerSchedules.Schedules, Is.Empty);
        });
    }

    private static MacroScenario CreateRunningScenario(
        IEnumerable<SpellSnapshot> spells,
        int currentMana = 500,
        ClientPanel activePanel = ClientPanel.TemuairSpells)
    {
        var scenario = new MacroScenario();
        scenario.Observe(
            sequence: 1,
            activePanel: activePanel,
            vitals: Vitals(currentMana),
            spellbook: new SpellbookSnapshot(spells),
            location: SourceLocation());
        scenario.Start();
        return scenario;
    }

    private static FlowerQueueEntry Entry(
        long id,
        SpellTarget target) =>
        new(
            new FlowerQueueEntryId(id),
            target,
            interval: TimeSpan.Zero);

    private static SpellSnapshot Spell(
        string name,
        int slot,
        int manaCost = 0,
        TimeSpan? cooldown = null) =>
        new(
            name,
            slot,
            currentLevel: 0,
            maximumLevel: 100,
            castLines: 1,
            manaCost,
            cooldown ?? TimeSpan.Zero);

    private static VitalsSnapshot Vitals(int currentMana = 500) =>
        new(
            currentHealth: 100,
            maximumHealth: 100,
            currentMana,
            maximumMana: 1000);

    private static ClientRosterEntry Client(
        string characterName,
        bool isWaitingForMana = false,
        MapLocationSnapshot? location = null,
        ClientIdentity? client = null) =>
        new(
            client ?? new ClientIdentity(characterName, "test"),
            characterName,
            ClientPresence.InWorld,
            isMacroRunning: true,
            isWaitingForMana,
            location,
            Vitals());

    private static MapLocationSnapshot SourceLocation() =>
        new(
            mapNumber: 1,
            mapName: "test map",
            x: 50,
            y: 50);

    private static MapLocationSnapshot NearbyLocation() =>
        new(
            mapNumber: 1,
            mapName: "test map",
            x: 55,
            y: 55);
}
