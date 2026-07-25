using SleepHunter.Runtime.Actions;
using SleepHunter.Runtime.Automation.Panels;
using SleepHunter.Runtime.Automation.Staves;
using SleepHunter.Runtime.Characters;
using SleepHunter.Runtime.Commands;
using SleepHunter.Runtime.Engine;
using SleepHunter.Runtime.Events;
using SleepHunter.Runtime.Intents;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Tests.Scenarios;

namespace SleepHunter.Runtime.Tests.Engine;

public sealed class StaffSwitchScenarioTests
{
    private static readonly StaffEquipmentPolicy TestPolicy = new(
        TimeSpan.FromMilliseconds(100),
        maximumAttempts: 2);

    [Test]
    public void ShouldNotIssueIntentForForeignClassStaff()
    {
        var foreign = Candidate(
            "foreign",
            CharacterClass.Priest,
            castLines: 0);
        var scenario = CreateRunningScenario(
            ClientPanel.Inventory,
            CharacterClass.Unknown,
            [new InventoryItemSnapshot(1, foreign.Name)]);

        var decision = scenario.Send(
            new RequestStaffSwitchCommand(
                baseCastLines: 4,
                [foreign],
                TestPolicy));

        Assert.Multiple(() =>
        {
            Assert.That(decision.Intent, Is.Null);
            Assert.That(decision.ScheduledEvents, Is.Empty);
            Assert.That(decision.State.PendingAction, Is.Null);
            Assert.That(
                decision.State.StaffSwitch?.Status,
                Is.EqualTo(StaffSwitchStatus.NoChange));
            Assert.That(
                decision.State.StaffSwitch?.Selection?.Reason,
                Is.EqualTo(StaffSelectionReason.NoEligibleStaff));
        });
    }

    [Test]
    public void ShouldExposeUnavailableSnapshotWithoutIntent()
    {
        var scenario = new MacroScenario();
        scenario.Observe(sequence: 1, activePanel: ClientPanel.Inventory);
        scenario.Start();

        var decision = scenario.Send(
            new RequestStaffSwitchCommand(
                baseCastLines: 4,
                candidates: [],
                TestPolicy));

        Assert.Multiple(() =>
        {
            Assert.That(decision.Intent, Is.Null);
            Assert.That(decision.State.PendingAction, Is.Null);
            Assert.That(
                decision.State.StaffSwitch?.Status,
                Is.EqualTo(StaffSwitchStatus.SnapshotUnavailable));
        });
    }

    [Test]
    public void ShouldIssueBoundedEquipmentIntentFromInventoryPanel()
    {
        var staff = Candidate(
            "staff",
            CharacterClass.Wizard,
            castLines: 1);
        var scenario = CreateRunningScenario(
            ClientPanel.Inventory,
            CharacterClass.Wizard,
            [new InventoryItemSnapshot(7, staff.Name)]);

        var decision = scenario.Send(
            new RequestStaffSwitchCommand(
                baseCastLines: 4,
                [staff],
                TestPolicy));
        var intent = decision.Intent as EquipWeaponIntent;

        Assert.Multiple(() =>
        {
            Assert.That(intent, Is.Not.Null);
            Assert.That(intent!.ActionId.Value, Is.EqualTo(1));
            Assert.That(intent.StaffName, Is.EqualTo(staff.Name));
            Assert.That(intent.InventorySlot, Is.EqualTo(7));
            Assert.That(decision.ScheduledEvents, Has.Length.EqualTo(1));
            Assert.That(decision.State.PendingAction?.Attempt, Is.EqualTo(1));
            Assert.That(
                decision.State.StaffSwitch?.Status,
                Is.EqualTo(StaffSwitchStatus.ChangingWeapon));
        });
    }

    [Test]
    public void ShouldPauseWhenWeaponIssuanceIsPartial()
    {
        var staff = Candidate(
            "staff",
            CharacterClass.Wizard,
            castLines: 1);
        var scenario = CreateRunningScenario(
            ClientPanel.Inventory,
            CharacterClass.Wizard,
            [new InventoryItemSnapshot(7, staff.Name)],
            issueActions: false);
        var requested = scenario.Send(
            new RequestStaffSwitchCommand(
                baseCastLines: 4,
                [staff],
                TestPolicy));

        var failed = scenario.Dispatch(
            new ClientActionIssueObserved(
                new ClientActionIssue(
                    ((EquipWeaponIntent)requested.Intent!).ActionId,
                    ClientActionIssueStatus.PartiallyIssued)));

        Assert.Multiple(() =>
        {
            Assert.That(failed.State.Lifecycle, Is.EqualTo(MacroLifecycle.Paused));
            Assert.That(failed.State.PendingAction, Is.Null);
            Assert.That(
                failed.State.StaffSwitch?.Status,
                Is.EqualTo(StaffSwitchStatus.IssueFailed));
        });
    }

    [Test]
    public void ShouldPauseWhenInventoryModeIssuanceIsRejected()
    {
        var staff = Candidate(
            "staff",
            CharacterClass.Wizard,
            castLines: 1);
        var scenario = CreateRunningScenario(
            ClientPanel.Inventory,
            CharacterClass.Wizard,
            [new InventoryItemSnapshot(35, staff.Name)],
            issueActions: false);
        var requested = scenario.Send(
            new RequestStaffSwitchCommand(
                baseCastLines: 4,
                [staff],
                TestPolicy));

        var failed = scenario.Dispatch(
            new ClientActionIssueObserved(
                new ClientActionIssue(
                    ((ExpandInventoryIntent)requested.Intent!).ActionId,
                    ClientActionIssueStatus.Rejected)));

        Assert.Multiple(() =>
        {
            Assert.That(failed.State.Lifecycle, Is.EqualTo(MacroLifecycle.Paused));
            Assert.That(
                failed.State.StaffSwitch?.Status,
                Is.EqualTo(StaffSwitchStatus.IssueFailed));
        });
    }

    [Test]
    public void ShouldExpandInventoryBeforeEquippingAnExtendedSlot()
    {
        var staff = Candidate(
            "staff",
            CharacterClass.Wizard,
            castLines: 1);
        var inventory = new InventorySnapshot(
        [
            new InventoryItemSnapshot(35, staff.Name)
        ]);
        var scenario = CreateRunningScenario(
            ClientPanel.Inventory,
            CharacterClass.Wizard,
            inventory.Items);

        var expand = scenario.Send(
            new RequestStaffSwitchCommand(
                baseCastLines: 4,
                [staff],
                TestPolicy));
        scenario.AdvanceBy(TimeSpan.FromTicks(1));
        var equip = scenario.Observe(
            sequence: 2,
            activePanel: ClientPanel.Inventory,
            character: Character(CharacterClass.Wizard),
            inventory: inventory,
            equipment: new EquipmentSnapshot(weaponName: null),
            isInventoryExpanded: true);

        Assert.Multiple(() =>
        {
            Assert.That(expand.Intent, Is.TypeOf<ExpandInventoryIntent>());
            Assert.That(expand.State.PendingAction?.Attempt, Is.EqualTo(1));
            Assert.That(
                expand.State.PendingAction?.MaximumAttempts,
                Is.EqualTo(1));
            Assert.That(
                expand.State.StaffSwitch?.Status,
                Is.EqualTo(StaffSwitchStatus.ChangingInventoryMode));
            Assert.That(
                expand.State.StaffSwitch?.TargetInventoryExpanded,
                Is.True);
            Assert.That(
                expand.State.StaffSwitch?.MaximumAttempts,
                Is.EqualTo(TestPolicy.MaximumAttempts));
            Assert.That(equip.Intent, Is.TypeOf<EquipWeaponIntent>());
            Assert.That(
                ((EquipWeaponIntent)equip.Intent!).InventorySlot,
                Is.EqualTo(35));
            Assert.That(equip.State.PendingAction?.Attempt, Is.EqualTo(1));
            Assert.That(
                equip.State.PendingAction?.MaximumAttempts,
                Is.EqualTo(TestPolicy.MaximumAttempts));
        });
    }

    [Test]
    public void ShouldCollapseInventoryBeforeEquippingACompactSlot()
    {
        var staff = Candidate(
            "staff",
            CharacterClass.Wizard,
            castLines: 1);
        var inventory = new InventorySnapshot(
        [
            new InventoryItemSnapshot(7, staff.Name)
        ]);
        var scenario = CreateRunningScenario(
            ClientPanel.Inventory,
            CharacterClass.Wizard,
            inventory.Items,
            isInventoryExpanded: true);

        var collapse = scenario.Send(
            new RequestStaffSwitchCommand(
                baseCastLines: 4,
                [staff],
                TestPolicy));
        scenario.AdvanceBy(TimeSpan.FromTicks(1));
        var equip = scenario.Observe(
            sequence: 2,
            activePanel: ClientPanel.Inventory,
            character: Character(CharacterClass.Wizard),
            inventory: inventory,
            equipment: new EquipmentSnapshot(weaponName: null));

        Assert.Multiple(() =>
        {
            Assert.That(
                collapse.Intent,
                Is.TypeOf<CollapseInventoryIntent>());
            Assert.That(
                collapse.State.StaffSwitch?.TargetInventoryExpanded,
                Is.False);
            Assert.That(equip.Intent, Is.TypeOf<EquipWeaponIntent>());
            Assert.That(
                ((EquipWeaponIntent)equip.Intent!).InventorySlot,
                Is.EqualTo(7));
        });
    }

    [Test]
    public void ShouldNotReplayAnUnconfirmedInventoryToggle()
    {
        var staff = Candidate(
            "staff",
            CharacterClass.Wizard,
            castLines: 1);
        var scenario = CreateRunningScenario(
            ClientPanel.Inventory,
            CharacterClass.Wizard,
            [new InventoryItemSnapshot(35, staff.Name)]);
        var expand = scenario.Send(
            new RequestStaffSwitchCommand(
                baseCastLines: 4,
                [staff],
                TestPolicy));

        scenario.AdvanceBy(TestPolicy.AttemptTimeout);
        var timedOut = scenario.Dispatch(
            expand.ScheduledEvents.Single().Input);

        Assert.Multiple(() =>
        {
            Assert.That(expand.Intent, Is.TypeOf<ExpandInventoryIntent>());
            Assert.That(timedOut.Intent, Is.Null);
            Assert.That(timedOut.State.PendingAction, Is.Null);
            Assert.That(
                timedOut.State.StaffSwitch?.Status,
                Is.EqualTo(StaffSwitchStatus.TimedOut));
        });
    }

    [Test]
    public void ShouldRestoreInventoryModeBeforeAnEquipmentRetry()
    {
        var staff = Candidate(
            "staff",
            CharacterClass.Wizard,
            castLines: 1);
        var inventory = new InventorySnapshot(
        [
            new InventoryItemSnapshot(7, staff.Name)
        ]);
        var scenario = CreateRunningScenario(
            ClientPanel.Inventory,
            CharacterClass.Wizard,
            inventory.Items);
        var firstEquip = scenario.Send(
            new RequestStaffSwitchCommand(
                baseCastLines: 4,
                [staff],
                TestPolicy));
        scenario.AdvanceBy(TimeSpan.FromTicks(1));
        scenario.Observe(
            sequence: 2,
            activePanel: ClientPanel.Inventory,
            character: Character(CharacterClass.Wizard),
            inventory: inventory,
            equipment: new EquipmentSnapshot(weaponName: null),
            isInventoryExpanded: true);
        scenario.AdvanceBy(
            TestPolicy.AttemptTimeout - TimeSpan.FromTicks(1));

        var collapse = scenario.Dispatch(
            firstEquip.ScheduledEvents.Single().Input);
        scenario.AdvanceBy(TimeSpan.FromTicks(1));
        var retryEquip = scenario.Observe(
            sequence: 3,
            activePanel: ClientPanel.Inventory,
            character: Character(CharacterClass.Wizard),
            inventory: inventory,
            equipment: new EquipmentSnapshot(weaponName: null));

        Assert.Multiple(() =>
        {
            Assert.That(firstEquip.Intent, Is.TypeOf<EquipWeaponIntent>());
            Assert.That(
                collapse.Intent,
                Is.TypeOf<CollapseInventoryIntent>());
            Assert.That(
                collapse.State.StaffSwitch?.CompletedEquipmentAttempts,
                Is.EqualTo(1));
            Assert.That(retryEquip.Intent, Is.TypeOf<EquipWeaponIntent>());
            Assert.That(retryEquip.State.PendingAction?.Attempt, Is.EqualTo(2));
            Assert.That(
                retryEquip.State.PendingAction?.MaximumAttempts,
                Is.EqualTo(TestPolicy.MaximumAttempts));
        });
    }

    [Test]
    public void ShouldSequenceInventoryPanelBeforeEquipmentIntent()
    {
        var staff = Candidate(
            "staff",
            CharacterClass.Wizard,
            castLines: 1);
        var inventory = new[]
        {
            new InventoryItemSnapshot(7, staff.Name)
        };
        var scenario = CreateRunningScenario(
            ClientPanel.Stats,
            CharacterClass.Wizard,
            inventory);

        var request = scenario.Send(
            new RequestStaffSwitchCommand(
                baseCastLines: 4,
                [staff],
                TestPolicy));
        scenario.AdvanceBy(TimeSpan.FromTicks(1));
        var panelConfirmed = scenario.Observe(
            sequence: 2,
            activePanel: ClientPanel.Inventory,
            character: Character(CharacterClass.Wizard),
            inventory: new InventorySnapshot(inventory),
            equipment: new EquipmentSnapshot(weaponName: null));
        scenario.AdvanceBy(TimeSpan.FromTicks(1));
        var equipmentConfirmed = scenario.Observe(
            sequence: 3,
            activePanel: ClientPanel.Inventory,
            character: Character(CharacterClass.Wizard),
            inventory: InventorySnapshot.Empty,
            equipment: new EquipmentSnapshot(staff.Name));

        Assert.Multiple(() =>
        {
            Assert.That(request.Intent, Is.TypeOf<SwitchPanelIntent>());
            Assert.That(
                request.State.StaffSwitch?.Status,
                Is.EqualTo(StaffSwitchStatus.WaitingForInventory));
            Assert.That(
                panelConfirmed.Intent,
                Is.TypeOf<EquipWeaponIntent>());
            Assert.That(
                ((EquipWeaponIntent)panelConfirmed.Intent!).ActionId.Value,
                Is.EqualTo(2));
            Assert.That(
                panelConfirmed.State.StaffSwitch?.Status,
                Is.EqualTo(StaffSwitchStatus.ChangingWeapon));
            Assert.That(equipmentConfirmed.State.PendingAction, Is.Null);
            Assert.That(
                equipmentConfirmed.State.StaffSwitch?.Status,
                Is.EqualTo(StaffSwitchStatus.Succeeded));
        });
    }

    [Test]
    public void ShouldStopWhenSelectionChangesBeforePanelConfirmation()
    {
        var staff = Candidate(
            "staff",
            CharacterClass.Wizard,
            castLines: 1);
        var scenario = CreateRunningScenario(
            ClientPanel.Stats,
            CharacterClass.Wizard,
            [new InventoryItemSnapshot(7, staff.Name)]);
        scenario.Send(
            new RequestStaffSwitchCommand(
                baseCastLines: 4,
                [staff],
                TestPolicy));
        scenario.AdvanceBy(TimeSpan.FromTicks(1));

        var panelConfirmed = scenario.Observe(
            sequence: 2,
            activePanel: ClientPanel.Inventory,
            character: Character(CharacterClass.Wizard),
            inventory: InventorySnapshot.Empty,
            equipment: new EquipmentSnapshot(weaponName: null));

        Assert.Multiple(() =>
        {
            Assert.That(panelConfirmed.Intent, Is.Null);
            Assert.That(panelConfirmed.State.PendingAction, Is.Null);
            Assert.That(
                panelConfirmed.State.StaffSwitch?.Status,
                Is.EqualTo(StaffSwitchStatus.SelectionInvalidated));
        });
    }

    [Test]
    public void ShouldRetryThenExposeStableEquipmentTimeout()
    {
        var staff = Candidate(
            "staff",
            CharacterClass.Wizard,
            castLines: 1);
        var scenario = CreateRunningScenario(
            ClientPanel.Inventory,
            CharacterClass.Wizard,
            [new InventoryItemSnapshot(7, staff.Name)]);
        var first = scenario.Send(
            new RequestStaffSwitchCommand(
                baseCastLines: 4,
                [staff],
                TestPolicy));

        scenario.AdvanceBy(TestPolicy.AttemptTimeout);
        var retry = scenario.Dispatch(first.ScheduledEvents.Single().Input);
        scenario.AdvanceBy(TestPolicy.AttemptTimeout);
        var timedOut = scenario.Dispatch(retry.ScheduledEvents.Single().Input);

        Assert.Multiple(() =>
        {
            Assert.That(retry.Intent, Is.TypeOf<EquipWeaponIntent>());
            Assert.That(
                ((EquipWeaponIntent)retry.Intent!).ActionId.Value,
                Is.EqualTo(2));
            Assert.That(retry.State.PendingAction?.Attempt, Is.EqualTo(2));
            Assert.That(timedOut.Intent, Is.Null);
            Assert.That(timedOut.State.PendingAction, Is.Null);
            Assert.That(
                timedOut.State.StaffSwitch?.Status,
                Is.EqualTo(StaffSwitchStatus.TimedOut));
        });
    }

    [Test]
    public void ShouldRejectEquipmentConfirmationFromStaleCapture()
    {
        var staff = Candidate(
            "staff",
            CharacterClass.Wizard,
            castLines: 1);
        var scenario = CreateRunningScenario(
            ClientPanel.Inventory,
            CharacterClass.Wizard,
            [new InventoryItemSnapshot(7, staff.Name)]);
        scenario.Send(
            new RequestStaffSwitchCommand(
                baseCastLines: 4,
                [staff],
                TestPolicy));

        var stale = scenario.Observe(
            sequence: 2,
            activePanel: ClientPanel.Inventory,
            character: Character(CharacterClass.Wizard),
            inventory: InventorySnapshot.Empty,
            equipment: new EquipmentSnapshot(staff.Name));
        scenario.AdvanceBy(TimeSpan.FromTicks(1));
        var confirmed = scenario.Observe(
            sequence: 3,
            activePanel: ClientPanel.Inventory,
            character: Character(CharacterClass.Wizard),
            inventory: InventorySnapshot.Empty,
            equipment: new EquipmentSnapshot(staff.Name));

        Assert.Multiple(() =>
        {
            Assert.That(stale.State.PendingAction, Is.Not.Null);
            Assert.That(
                stale.State.StaffSwitch?.Status,
                Is.EqualTo(StaffSwitchStatus.ChangingWeapon));
            Assert.That(confirmed.State.PendingAction, Is.Null);
            Assert.That(
                confirmed.State.StaffSwitch?.Status,
                Is.EqualTo(StaffSwitchStatus.Succeeded));
        });
    }

    [Test]
    public void ShouldExposePanelFailureWithoutEquipmentIntent()
    {
        var staff = Candidate(
            "staff",
            CharacterClass.Wizard,
            castLines: 1);
        var scenario = CreateRunningScenario(
            ClientPanel.Stats,
            CharacterClass.Wizard,
            [new InventoryItemSnapshot(7, staff.Name)]);
        var first = scenario.Send(
            new RequestStaffSwitchCommand(
                baseCastLines: 4,
                [staff],
                TestPolicy));

        scenario.AdvanceBy(TestPolicy.AttemptTimeout);
        var retry = scenario.Dispatch(first.ScheduledEvents.Single().Input);
        scenario.AdvanceBy(TestPolicy.AttemptTimeout);
        var failed = scenario.Dispatch(retry.ScheduledEvents.Single().Input);

        Assert.Multiple(() =>
        {
            Assert.That(first.Intent, Is.TypeOf<SwitchPanelIntent>());
            Assert.That(retry.Intent, Is.TypeOf<SwitchPanelIntent>());
            Assert.That(failed.Intent, Is.Null);
            Assert.That(failed.State.PendingAction, Is.Null);
            Assert.That(
                failed.State.StaffSwitch?.Status,
                Is.EqualTo(StaffSwitchStatus.PanelUnavailable));
        });
    }

    [Test]
    public void ShouldReacquireInventoryPanelBeforeEquipmentRetry()
    {
        var staff = Candidate(
            "staff",
            CharacterClass.Wizard,
            castLines: 1);
        var inventory = new InventorySnapshot(
        [
            new InventoryItemSnapshot(7, staff.Name)
        ]);
        var scenario = CreateRunningScenario(
            ClientPanel.Inventory,
            CharacterClass.Wizard,
            inventory.Items);
        var first = scenario.Send(
            new RequestStaffSwitchCommand(
                baseCastLines: 4,
                [staff],
                TestPolicy));
        scenario.AdvanceBy(TimeSpan.FromTicks(1));
        scenario.Observe(
            sequence: 2,
            activePanel: ClientPanel.Stats,
            character: Character(CharacterClass.Wizard),
            inventory: inventory,
            equipment: new EquipmentSnapshot(weaponName: null));
        scenario.AdvanceBy(
            TestPolicy.AttemptTimeout - TimeSpan.FromTicks(1));

        var panelRequest = scenario.Dispatch(
            first.ScheduledEvents.Single().Input);
        scenario.AdvanceBy(TimeSpan.FromTicks(1));
        var panelConfirmed = scenario.Observe(
            sequence: 3,
            activePanel: ClientPanel.Inventory,
            character: Character(CharacterClass.Wizard),
            inventory: inventory,
            equipment: new EquipmentSnapshot(weaponName: null));

        Assert.Multiple(() =>
        {
            Assert.That(panelRequest.Intent, Is.TypeOf<SwitchPanelIntent>());
            Assert.That(
                panelRequest.State.StaffSwitch?.Status,
                Is.EqualTo(StaffSwitchStatus.WaitingForInventory));
            Assert.That(
                panelConfirmed.Intent,
                Is.TypeOf<EquipWeaponIntent>());
            Assert.That(panelConfirmed.State.PendingAction?.Attempt, Is.EqualTo(2));
            Assert.That(
                ((EquipWeaponIntent)panelConfirmed.Intent!).ActionId.Value,
                Is.EqualTo(3));
        });
    }

    [Test]
    public void ShouldUnequipWithoutInventoryPanelAndConfirmFromSnapshot()
    {
        var staff = Candidate(
            "staff",
            CharacterClass.Wizard,
            castLines: 5);
        var scenario = CreateRunningScenario(
            ClientPanel.Stats,
            CharacterClass.Wizard,
            inventory: [],
            equippedWeapon: staff.Name);

        var request = scenario.Send(
            new RequestStaffSwitchCommand(
                baseCastLines: 4,
                [staff],
                TestPolicy));
        scenario.AdvanceBy(TimeSpan.FromTicks(1));
        var confirmed = scenario.Observe(
            sequence: 2,
            activePanel: ClientPanel.Stats,
            character: Character(CharacterClass.Wizard),
            inventory: InventorySnapshot.Empty,
            equipment: new EquipmentSnapshot(weaponName: null));

        Assert.Multiple(() =>
        {
            Assert.That(request.Intent, Is.TypeOf<EquipWeaponIntent>());
            Assert.That(
                ((EquipWeaponIntent)request.Intent!).IsUnequip,
                Is.True);
            Assert.That(confirmed.State.PendingAction, Is.Null);
            Assert.That(
                confirmed.State.StaffSwitch?.Status,
                Is.EqualTo(StaffSwitchStatus.Succeeded));
        });
    }

    [Test]
    public void ShouldCancelPendingStaffSwitchWhenPaused()
    {
        var staff = Candidate(
            "staff",
            CharacterClass.Wizard,
            castLines: 1);
        var scenario = CreateRunningScenario(
            ClientPanel.Inventory,
            CharacterClass.Wizard,
            [new InventoryItemSnapshot(7, staff.Name)]);
        var request = scenario.Send(
            new RequestStaffSwitchCommand(
                baseCastLines: 4,
                [staff],
                TestPolicy));

        var paused = scenario.Pause();
        scenario.AdvanceBy(TestPolicy.AttemptTimeout);
        var staleDeadline = scenario.Dispatch(
            request.ScheduledEvents.Single().Input);

        Assert.Multiple(() =>
        {
            Assert.That(paused.State.PendingAction, Is.Null);
            Assert.That(
                paused.State.StaffSwitch?.Status,
                Is.EqualTo(StaffSwitchStatus.Cancelled));
            Assert.That(staleDeadline.State, Is.SameAs(paused.State));
            Assert.That(staleDeadline.Intent, Is.Null);
        });
    }

    [Test]
    public void ShouldCancelStaffSwitchWhenStoppedOrLoggedOut()
    {
        var staff = Candidate(
            "staff",
            CharacterClass.Wizard,
            castLines: 1);
        var stoppedScenario = CreateRunningScenario(
            ClientPanel.Stats,
            CharacterClass.Wizard,
            [new InventoryItemSnapshot(7, staff.Name)]);
        stoppedScenario.Send(
            new RequestStaffSwitchCommand(
                baseCastLines: 4,
                [staff],
                TestPolicy));
        var stopped = stoppedScenario.Stop();

        var logoutScenario = CreateRunningScenario(
            ClientPanel.Inventory,
            CharacterClass.Wizard,
            [new InventoryItemSnapshot(7, staff.Name)]);
        logoutScenario.Send(
            new RequestStaffSwitchCommand(
                baseCastLines: 4,
                [staff],
                TestPolicy));
        logoutScenario.AdvanceBy(TimeSpan.FromTicks(1));
        var loggedOut = logoutScenario.Observe(
            sequence: 2,
            presence: ClientPresence.LoggedOut,
            activePanel: ClientPanel.Unknown);

        Assert.Multiple(() =>
        {
            Assert.That(stopped.State.PendingAction, Is.Null);
            Assert.That(
                stopped.State.StaffSwitch?.Status,
                Is.EqualTo(StaffSwitchStatus.Cancelled));
            Assert.That(loggedOut.State.PendingAction, Is.Null);
            Assert.That(
                loggedOut.State.StaffSwitch?.Status,
                Is.EqualTo(StaffSwitchStatus.Cancelled));
            Assert.That(
                loggedOut.State.StopReason,
                Is.EqualTo(MacroStopReason.ClientLoggedOut));
        });
    }

    [Test]
    public void ShouldValidateStaffEquipmentInputs()
    {
        var staff = Candidate(
            "staff",
            CharacterClass.Wizard,
            castLines: 1);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = new StaffEquipmentPolicy(
                    TimeSpan.Zero,
                    maximumAttempts: 1));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = new StaffEquipmentPolicy(
                    TimeSpan.FromSeconds(1),
                    maximumAttempts: 0));
            Assert.Throws<ArgumentException>(
                () => _ = new EquipWeaponIntent(
                    new ClientActionId(1),
                    staff.Name,
                    inventorySlot: null));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = new EquipWeaponIntent(
                    new ClientActionId(1),
                    staff.Name,
                    InventoryItemSnapshot.MaximumSlot));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = new RequestStaffSwitchCommand(
                    baseCastLines: -1,
                    [staff]));
        });
    }

    [Test]
    public void ShouldNotSupersedeInventoryPanelOwnedByStaffSwitch()
    {
        var scenario = CreateRunningScenario(
            ClientPanel.Stats,
            CharacterClass.Wizard,
            inventory:
            [
                new InventoryItemSnapshot(7, "staff")
            ]);
        var staff = Candidate(
            "staff",
            CharacterClass.Wizard,
            castLines: 1);
        var switching = scenario.Send(
            new RequestStaffSwitchCommand(
                baseCastLines: 4,
                [staff],
                TestPolicy));

        var manual = scenario.Send(
            new RequestPanelTransitionCommand(
                ClientPanel.TemuairSpells));

        Assert.Multiple(() =>
        {
            Assert.That(manual.State, Is.SameAs(switching.State));
            Assert.That(manual.Intent, Is.Null);
            Assert.That(manual.PublishedView, Is.Null);
            Assert.That(
                ((SwitchPanelIntent)switching.Intent!).TargetPanel,
                Is.EqualTo(ClientPanel.Inventory));
        });
    }

    private static MacroScenario CreateRunningScenario(
        ClientPanel activePanel,
        CharacterClass characterClass,
        IEnumerable<InventoryItemSnapshot> inventory,
        string? equippedWeapon = null,
        bool isInventoryExpanded = false,
        bool issueActions = true)
    {
        var scenario = new MacroScenario(issueActions: issueActions);
        scenario.Observe(
            sequence: 1,
            activePanel: activePanel,
            character: Character(characterClass),
            inventory: new InventorySnapshot(inventory),
            equipment: new EquipmentSnapshot(equippedWeapon),
            isInventoryExpanded: isInventoryExpanded);
        scenario.Start();
        return scenario;
    }

    private static CharacterSnapshot Character(CharacterClass characterClass) =>
        new(characterClass, level: 99, abilityLevel: 99);

    private static StaffCandidate Candidate(
        string name,
        CharacterClass? requiredClass,
        int castLines) =>
        new(
            name,
            requiredClass,
            requiredLevel: 0,
            requiredAbilityLevel: 0,
            castLines);
}
