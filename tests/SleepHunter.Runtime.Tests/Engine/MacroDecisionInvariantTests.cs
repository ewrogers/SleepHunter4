using System.Collections.Immutable;

using SleepHunter.Runtime.Actions;
using SleepHunter.Runtime.Automation.Dialogs;
using SleepHunter.Runtime.Automation.Panels;
using SleepHunter.Runtime.Automation.Spells;
using SleepHunter.Runtime.Automation.Staves;
using SleepHunter.Runtime.Characters;
using SleepHunter.Runtime.Engine;
using SleepHunter.Runtime.Events;
using SleepHunter.Runtime.Intents;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Tests.Hosting;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Tests.Engine;

public sealed class MacroDecisionInvariantTests
{
    [Test]
    public void ShouldRejectClientIntentWithoutMatchingPendingAction()
    {
        var pendingIntent =
            new TestClientActionIntent(new ClientActionId(1));
        var emittedIntent =
            new TestClientActionIntent(new ClientActionId(2));
        var state = new MacroState(
            revision: 1,
            MacroLifecycle.Running,
            MacroStopReason.None,
            latestSnapshot: null,
            MacroTimestamp.Zero,
            new PendingAction(
                pendingIntent,
                MacroTimestamp.Zero,
                new MacroTimestamp(TimeSpan.FromSeconds(1)),
                attempt: 1));
        var decision = new MacroDecision(
            state,
            ImmutableArray<MacroEvent>.Empty,
            ImmutableArray<ScheduledMacroEvent>.Empty,
            emittedIntent,
            publishedView: null);

        Assert.That(
            () => MacroDecisionInvariants.EnsureValid(
                state,
                decision,
                MacroTimestamp.Zero),
            Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void ShouldRejectScheduledEventEarlierThanCurrentTime()
    {
        var decision = new MacroDecision(
            MacroState.Initial,
            ImmutableArray<MacroEvent>.Empty,
            [
                new ScheduledMacroEvent(
                    new TestDeadlineEvent(1),
                    MacroTimestamp.Zero)
            ],
            intent: null,
            publishedView: null);

        Assert.That(
            () => MacroDecisionInvariants.EnsureValid(
                MacroState.Initial,
                decision,
                new MacroTimestamp(TimeSpan.FromTicks(1))),
            Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void ShouldRejectPanelIntentWithoutMatchingTransitionState()
    {
        var intent = new SwitchPanelIntent(
            new ClientActionId(1),
            ClientPanel.TemuairSpells);
        var pendingAction = new PendingAction(
            intent,
            MacroTimestamp.Zero,
            new MacroTimestamp(TimeSpan.FromSeconds(1)),
            attempt: 1);
        var state = new MacroState(
            revision: 1,
            MacroLifecycle.Running,
            MacroStopReason.None,
            latestSnapshot: null,
            MacroTimestamp.Zero,
            pendingAction);
        var decision = new MacroDecision(
            state,
            ImmutableArray<MacroEvent>.Empty,
            ImmutableArray<ScheduledMacroEvent>.Empty,
            intent: null,
            publishedView: null);

        Assert.That(
            () => MacroDecisionInvariants.EnsureValid(
                state,
                decision,
                MacroTimestamp.Zero),
            Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void ShouldRejectWeaponIntentWithoutMatchingStaffState()
    {
        var staff = new StaffCandidate(
            "staff",
            CharacterClass.Wizard,
            requiredLevel: 0,
            requiredAbilityLevel: 0,
            castLines: 1);
        var selection = new StaffSelection(
            StaffSelectionAction.Equip,
            StaffSelectionReason.BetterStaffAvailable,
            castLines: 1,
            staff,
            inventorySlot: 7);
        var intent = new EquipWeaponIntent(
            new ClientActionId(1),
            staff.Name,
            inventorySlot: 7);
        var pendingAction = new PendingAction(
            intent,
            MacroTimestamp.Zero,
            new MacroTimestamp(TimeSpan.FromSeconds(1)),
            attempt: 1);
        var staffSwitch = StaffSwitchState.ChangingWeapon(
            selection,
            TimeSpan.FromSeconds(1),
            attempt: 1,
            maximumAttempts: 1,
            new ClientActionId(2));
        var state = new MacroState(
            revision: 1,
            MacroLifecycle.Running,
            MacroStopReason.None,
            latestSnapshot: null,
            MacroTimestamp.Zero,
            pendingAction,
            staffSwitch: staffSwitch);
        var decision = new MacroDecision(
            state,
            ImmutableArray<MacroEvent>.Empty,
            ImmutableArray<ScheduledMacroEvent>.Empty,
            intent: null,
            publishedView: null);

        Assert.That(
            () => MacroDecisionInvariants.EnsureValid(
                state,
                decision,
                MacroTimestamp.Zero),
            Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void ShouldRejectCastIntentWithoutMatchingSpellState()
    {
        var entry = new SpellQueueEntry(
            new SpellQueueEntryId(1),
            "spell");
        var spell = new SpellSnapshot(
            "spell",
            slot: 1,
            currentLevel: 0,
            maximumLevel: 100,
            castLines: 1,
            manaCost: 0,
            cooldown: TimeSpan.Zero);
        var queue = SpellQueueState.Empty.Add(entry);
        var plan = SpellPlanner.Plan(
            new SpellPlanningRequest(
                queue,
                new VitalsSnapshot(100, 100, 100, 100),
                new SpellbookSnapshot([spell]),
                SpellCooldownState.Empty,
                MacroTimestamp.Zero));
        var deadline = new MacroTimestamp(TimeSpan.FromSeconds(1));
        var intent = new CastSpellIntent(
            new ClientActionId(1),
            spell.Name,
            spell.Slot,
            spell.Panel,
            entry.Target);
        var pendingAction = new PendingAction(
            intent,
            MacroTimestamp.Zero,
            deadline,
            attempt: 1);
        var spellCast = SpellCastState
            .FromPlan(plan, SpellExecutionPolicy.Default)
            .Casting(plan, new ClientActionId(2), deadline);
        var state = new MacroState(
            revision: 1,
            MacroLifecycle.Running,
            MacroStopReason.None,
            latestSnapshot: null,
            MacroTimestamp.Zero,
            pendingAction,
            spellCast: spellCast);
        var decision = new MacroDecision(
            state,
            ImmutableArray<MacroEvent>.Empty,
            ImmutableArray<ScheduledMacroEvent>.Empty,
            intent: null,
            publishedView: null);

        Assert.That(
            () => MacroDecisionInvariants.EnsureValid(
                state,
                decision,
                MacroTimestamp.Zero),
            Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void ShouldRejectDialogIntentWithoutMatchingDialogState()
    {
        var intent = new CancelDialogIntent(new ClientActionId(1));
        var pendingAction = new PendingAction(
            intent,
            MacroTimestamp.Zero,
            new MacroTimestamp(TimeSpan.FromSeconds(1)),
            attempt: 1);
        var state = new MacroState(
            revision: 1,
            MacroLifecycle.Running,
            MacroStopReason.None,
            latestSnapshot: null,
            MacroTimestamp.Zero,
            pendingAction);
        var decision = new MacroDecision(
            state,
            ImmutableArray<MacroEvent>.Empty,
            ImmutableArray<ScheduledMacroEvent>.Empty,
            intent: null,
            publishedView: null);

        Assert.That(
            () => MacroDecisionInvariants.EnsureValid(
                state,
                decision,
                MacroTimestamp.Zero),
            Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void ShouldRejectTileIntentWithoutMatchingWaterBedState()
    {
        var intent = new ClickTileIntent(
            new ClientActionId(1),
            new MapLocationSnapshot(
                mapNumber: 1,
                mapName: "test map",
                x: 5,
                y: 1));
        var pendingAction = new PendingAction(
            intent,
            MacroTimestamp.Zero,
            new MacroTimestamp(TimeSpan.FromSeconds(1)),
            attempt: 1);
        var state = new MacroState(
            revision: 1,
            MacroLifecycle.Running,
            MacroStopReason.None,
            latestSnapshot: null,
            MacroTimestamp.Zero,
            pendingAction);
        var decision = new MacroDecision(
            state,
            ImmutableArray<MacroEvent>.Empty,
            ImmutableArray<ScheduledMacroEvent>.Empty,
            intent: null,
            publishedView: null);

        Assert.That(
            () => MacroDecisionInvariants.EnsureValid(
                state,
                decision,
                MacroTimestamp.Zero),
            Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void ShouldRejectNewScheduledDialogWithoutMatchingEvent()
    {
        var previousState = new MacroState(
            revision: 1,
            MacroLifecycle.Running,
            MacroStopReason.None,
            latestSnapshot: null,
            MacroTimestamp.Zero,
            pendingAction: null);
        var dueAt = new MacroTimestamp(TimeSpan.FromSeconds(1));
        var state = new MacroState(
            revision: 2,
            MacroLifecycle.Running,
            MacroStopReason.None,
            latestSnapshot: null,
            MacroTimestamp.Zero,
            pendingAction: null,
            dialog: DialogState.Scheduled(DialogPolicy.Default, dueAt));
        var decision = new MacroDecision(
            state,
            ImmutableArray<MacroEvent>.Empty,
            ImmutableArray<ScheduledMacroEvent>.Empty,
            intent: null,
            MacroViewSnapshot.FromState(state));

        Assert.That(
            () => MacroDecisionInvariants.EnsureValid(
                previousState,
                decision,
                MacroTimestamp.Zero),
            Throws.TypeOf<InvalidOperationException>());
    }
}
