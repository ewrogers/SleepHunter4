using System.Collections.Immutable;
using SleepHunter.Runtime.Actions;
using SleepHunter.Runtime.Automation;
using SleepHunter.Runtime.Automation.Dialogs;
using SleepHunter.Runtime.Automation.Equipment;
using SleepHunter.Runtime.Automation.Flowering;
using SleepHunter.Runtime.Automation.Panels;
using SleepHunter.Runtime.Automation.Skills;
using SleepHunter.Runtime.Automation.Spells;
using SleepHunter.Runtime.Automation.Staves;
using SleepHunter.Runtime.Commands;
using SleepHunter.Runtime.Events;
using SleepHunter.Runtime.Intents;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Engine;

public sealed partial class MacroEngine : IMacroEngine
{
    public MacroDecision Decide(
        MacroState currentState,
        MacroEvent input,
        MacroTimestamp currentTime)
    {
        ArgumentNullException.ThrowIfNull(currentState);
        ArgumentNullException.ThrowIfNull(input);

        var decision = input switch
        {
            MacroCommandReceived commandReceived =>
                HandleCommand(currentState, commandReceived.Command, currentTime),
            ClientSnapshotObserved snapshotObserved =>
                HandleSnapshot(currentState, snapshotObserved.Snapshot, currentTime),
            ClientActionDeadlineElapsed deadlineElapsed =>
                HandleClientActionDeadline(
                    currentState,
                    deadlineElapsed,
                    currentTime),
            DialogCloseDue dialogCloseDue =>
                HandleDialogCloseDue(
                    currentState,
                    dialogCloseDue,
                    currentTime),
            ClientRosterObserved clientRosterObserved =>
                HandleClientRoster(
                    currentState,
                    clientRosterObserved.Snapshot,
                    currentTime),
            ClientActionIssueObserved issueObserved =>
                HandleClientActionIssue(
                    currentState,
                    issueObserved.Issue,
                    issueObserved.ObservedAt ?? currentTime),
            AutomationCycleRequested =>
                RunAutomationCycle(currentState, currentTime),
            _ => throw new ArgumentOutOfRangeException(
                nameof(input),
                input,
                "Unsupported macro event.")
        };

        decision = RequestAutomationCycleAfter(
            currentState,
            input,
            decision);
        MacroDecisionInvariants.EnsureValid(currentState, decision, currentTime);
        return decision;
    }

    private static MacroDecision HandleCommand(
        MacroState currentState,
        MacroCommand command,
        MacroTimestamp currentTime)
    {
        ArgumentNullException.ThrowIfNull(command);

        return command switch
        {
            StartMacroCommand => Start(currentState, currentTime),
            PauseMacroCommand => ChangeLifecycle(
                currentState,
                MacroLifecycle.Running,
                MacroLifecycle.Paused,
                MacroStopReason.None,
                currentTime),
            ResumeMacroCommand => ChangeLifecycle(
                currentState,
                MacroLifecycle.Paused,
                MacroLifecycle.Running,
                MacroStopReason.None,
                currentTime),
            StopMacroCommand => Stop(currentState, currentTime),
            ApplyAutomationSetupCommand applyAutomation =>
                ApplyAutomationSetup(
                    currentState,
                    applyAutomation,
                    currentTime),
            ConfigureAutomationCommand configureAutomation =>
                ConfigureAutomation(
                    currentState,
                    configureAutomation),
            RequestPanelTransitionCommand requestPanel =>
                RequestPanelTransition(
                    currentState,
                    requestPanel,
                    currentTime),
            RequestStaffSwitchCommand requestStaff =>
                RequestStaffSwitch(
                    currentState,
                    requestStaff,
                    currentTime),
            CastNextSpellCommand castNextSpell =>
                CastNextSpell(
                    currentState,
                    castNextSpell,
                    currentTime),
            UseNextSkillCommand useNextSkill =>
                UseNextSkill(
                    currentState,
                    useNextSkill,
                    currentTime),
            FlowerCommand flower =>
                Flower(
                    currentState,
                    flower,
                    currentTime),
            ReplaceQueuesCommand replaceQueues => ChangeQueues(
                currentState,
                replaceQueues,
                currentTime),
            ReplaceSpellQueueCommand replaceSpells => ChangeSpellQueue(
                currentState,
                replaceSpells.Queue),
            AddSpellQueueEntryCommand addEntry => ChangeSpellQueue(
                currentState,
                currentState.SpellQueue.Add(addEntry.Entry, addEntry.Index)),
            UpdateSpellQueueEntryCommand updateEntry => ChangeSpellQueue(
                currentState,
                currentState.SpellQueue.Update(updateEntry.Entry)),
            RemoveSpellQueueEntryCommand removeEntry => ChangeSpellQueue(
                currentState,
                currentState.SpellQueue.Remove(removeEntry.EntryId)),
            MoveSpellQueueEntryCommand moveEntry => ChangeSpellQueue(
                currentState,
                currentState.SpellQueue.Move(
                    moveEntry.EntryId,
                    moveEntry.TargetIndex)),
            ClearSpellQueueCommand => ChangeSpellQueue(
                currentState,
                currentState.SpellQueue.Clear()),
            SetSpellQueueRotationCommand setRotation => ChangeSpellQueue(
                currentState,
                currentState.SpellQueue.SetRotation(setRotation.Rotation)),
            ReplaceSkillQueueCommand replaceSkills => ChangeSkillQueue(
                currentState,
                replaceSkills.Queue),
            AddSkillQueueEntryCommand addSkill => ChangeSkillQueue(
                currentState,
                currentState.SkillQueue.Add(addSkill.Entry, addSkill.Index)),
            UpdateSkillQueueEntryCommand updateSkill => ChangeSkillQueue(
                currentState,
                currentState.SkillQueue.Update(updateSkill.Entry)),
            RemoveSkillQueueEntryCommand removeSkill => ChangeSkillQueue(
                currentState,
                currentState.SkillQueue.Remove(removeSkill.EntryId)),
            MoveSkillQueueEntryCommand moveSkill => ChangeSkillQueue(
                currentState,
                currentState.SkillQueue.Move(
                    moveSkill.EntryId,
                    moveSkill.TargetIndex)),
            ClearSkillQueueCommand => ChangeSkillQueue(
                currentState,
                currentState.SkillQueue.Clear()),
            ReplaceFlowerQueueCommand replaceFlowers => ChangeFlowerQueue(
                currentState,
                replaceFlowers.Queue,
                currentTime),
            AddFlowerQueueEntryCommand addFlower => ChangeFlowerQueue(
                currentState,
                currentState.FlowerQueue.Add(
                    addFlower.Entry,
                    addFlower.Index),
                currentTime),
            UpdateFlowerQueueEntryCommand updateFlower => ChangeFlowerQueue(
                currentState,
                currentState.FlowerQueue.Update(updateFlower.Entry),
                currentTime),
            RemoveFlowerQueueEntryCommand removeFlower => ChangeFlowerQueue(
                currentState,
                currentState.FlowerQueue.Remove(removeFlower.EntryId),
                currentTime),
            MoveFlowerQueueEntryCommand moveFlower => ChangeFlowerQueue(
                currentState,
                currentState.FlowerQueue.Move(
                    moveFlower.EntryId,
                    moveFlower.TargetIndex),
                currentTime),
            ClearFlowerQueueCommand => ChangeFlowerQueue(
                currentState,
                currentState.FlowerQueue.Clear(),
                currentTime),
            _ => throw new ArgumentOutOfRangeException(
                nameof(command),
                command,
                "Unsupported macro command.")
        };
    }

    private static MacroDecision Start(
        MacroState currentState,
        MacroTimestamp currentTime)
    {
        if (currentState.Lifecycle != MacroLifecycle.Stopped)
        {
            return Unchanged(currentState);
        }

        if (currentState.LatestSnapshot is not { Presence: ClientPresence.InWorld })
        {
            return Unchanged(currentState);
        }

        return Changed(
            currentState,
            MacroLifecycle.Running,
            MacroStopReason.None,
            currentState.LatestSnapshot,
            currentTime,
            pendingAction: null);
    }

    private static MacroDecision Stop(
        MacroState currentState,
        MacroTimestamp currentTime)
    {
        if (currentState.Lifecycle == MacroLifecycle.Stopped)
        {
            return Unchanged(currentState);
        }

        return Changed(
            currentState,
            MacroLifecycle.Stopped,
            MacroStopReason.UserRequested,
            currentState.LatestSnapshot,
            currentTime,
            pendingAction: null,
            panelTransition: CancelPendingPanelTransition(currentState),
            staffSwitch: CancelPendingStaffSwitch(currentState),
            spellCast: CancelPendingSpellCast(currentState),
            skillUse: CancelPendingSkillUse(currentState),
            disarm: CancelPendingDisarm(currentState),
            dialog: CancelPendingDialog(currentState),
            flower: CancelPendingFlower(currentState),
            panelPreservation:
                CancelPendingPanelPreservation(currentState));
    }

    private static MacroDecision ChangeLifecycle(
        MacroState currentState,
        MacroLifecycle requiredLifecycle,
        MacroLifecycle nextLifecycle,
        MacroStopReason stopReason,
        MacroTimestamp currentTime)
    {
        if (currentState.Lifecycle != requiredLifecycle)
        {
            return Unchanged(currentState);
        }

        return Changed(
            currentState,
            nextLifecycle,
            stopReason,
            currentState.LatestSnapshot,
            currentTime,
            pendingAction: nextLifecycle == MacroLifecycle.Running
                ? currentState.PendingAction
                : null,
            panelTransition: nextLifecycle == MacroLifecycle.Running
                ? currentState.PanelTransition
                : CancelPendingPanelTransition(currentState),
            staffSwitch: nextLifecycle == MacroLifecycle.Running
                ? currentState.StaffSwitch
                : CancelPendingStaffSwitch(currentState),
            spellCast: nextLifecycle == MacroLifecycle.Running
                ? currentState.SpellCast
                : CancelPendingSpellCast(currentState),
            skillUse: nextLifecycle == MacroLifecycle.Running
                ? currentState.SkillUse
                : CancelPendingSkillUse(currentState),
            disarm: nextLifecycle == MacroLifecycle.Running
                ? currentState.Disarm
                : CancelPendingDisarm(currentState),
            dialog: nextLifecycle == MacroLifecycle.Running
                ? currentState.Dialog
                : CancelPendingDialog(currentState),
            flower: nextLifecycle == MacroLifecycle.Running
                ? currentState.Flower
                : CancelPendingFlower(currentState),
            panelPreservation: nextLifecycle == MacroLifecycle.Running
                ? currentState.PanelPreservation
                : CancelPendingPanelPreservation(currentState));
    }

    private static MacroDecision HandleSnapshot(
        MacroState currentState,
        ClientSnapshot snapshot,
        MacroTimestamp currentTime)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (!snapshot.IsUsable || snapshot.CaptureCompletedAt > currentTime)
        {
            return Unchanged(currentState);
        }

        if (currentState.LatestSnapshot is { } latestSnapshot &&
            (snapshot.Client != latestSnapshot.Client ||
             snapshot.Sequence <= latestSnapshot.Sequence))
        {
            return Unchanged(currentState);
        }

        var clientLoggedOut =
            snapshot.Presence != ClientPresence.InWorld &&
            currentState.Lifecycle != MacroLifecycle.Stopped;

        var lifecycle = clientLoggedOut
            ? MacroLifecycle.Stopped
            : currentState.Lifecycle;
        var stopReason = clientLoggedOut
            ? MacroStopReason.ClientLoggedOut
            : currentState.StopReason;
        var lastTransitionAt = clientLoggedOut
            ? currentTime
            : currentState.LastTransitionAt;
        var pendingAction = clientLoggedOut
            ? null
            : currentState.PendingAction;
        var panelTransition = clientLoggedOut
            ? CancelPendingPanelTransition(currentState)
            : currentState.PanelTransition;
        var staffSwitch = clientLoggedOut
            ? CancelPendingStaffSwitch(currentState)
            : currentState.StaffSwitch;
        var spellCast = clientLoggedOut
            ? CancelPendingSpellCast(currentState)
            : currentState.SpellCast;
        var skillUse = clientLoggedOut
            ? CancelPendingSkillUse(currentState)
            : currentState.SkillUse;
        var disarm = clientLoggedOut
            ? CancelPendingDisarm(currentState)
            : currentState.Disarm;
        var dialog = clientLoggedOut
            ? CancelPendingDialog(currentState)
            : currentState.Dialog;
        var flower = clientLoggedOut
            ? CancelPendingFlower(currentState)
            : currentState.Flower;
        var panelPreservation = clientLoggedOut
            ? CancelPendingPanelPreservation(currentState)
            : currentState.PanelPreservation;

        if (!clientLoggedOut &&
            TryGetObservationChange(
                currentState,
                snapshot,
                out var changeAction,
                out var changeReason) &&
            changeAction != ObservationChangeAction.Continue)
        {
            var nextLifecycle = changeAction == ObservationChangeAction.Pause
                ? MacroLifecycle.Paused
                : MacroLifecycle.Stopped;

            return Changed(
                currentState,
                nextLifecycle,
                nextLifecycle == MacroLifecycle.Stopped
                    ? changeReason
                    : MacroStopReason.None,
                snapshot,
                currentTime,
                pendingAction: null,
                panelTransition: CancelPendingPanelTransition(currentState),
                staffSwitch: CancelPendingStaffSwitch(currentState),
                spellCast: CancelPendingSpellCast(currentState),
                skillUse: CancelPendingSkillUse(currentState),
                disarm: CancelPendingDisarm(currentState),
                dialog: CancelPendingDialog(currentState),
                flower: CancelPendingFlower(currentState),
                panelPreservation:
                    CancelPendingPanelPreservation(currentState));
        }

        if (!clientLoggedOut &&
            CanConfirmPanelTransition(currentState.PendingAction, snapshot))
        {
            var switchIntent = (SwitchPanelIntent)currentState.PendingAction!.Intent;
            panelTransition = PanelTransitionState.Succeeded(
                switchIntent.TargetPanel,
                currentState.PendingAction.Attempt,
                currentState.PendingAction.MaximumAttempts,
                switchIntent.ActionId);
            pendingAction = null;

            if (panelPreservation is
                {
                    Status: PanelPreservationStatus.Restoring
                } restoring &&
                switchIntent.TargetPanel.IsEquivalentTo(
                    restoring.OriginalPanel))
            {
                return Changed(
                    currentState,
                    currentState.Lifecycle,
                    currentState.StopReason,
                    snapshot,
                    currentState.LastTransitionAt,
                    pendingAction: null,
                    panelTransition: panelTransition,
                    panelPreservation: restoring.Succeeded());
            }

            if (staffSwitch is
                {
                    Status: StaffSwitchStatus.WaitingForInventory,
                    Selection: not null
                } &&
                switchIntent.TargetPanel == ClientPanel.Inventory)
            {
                return ContinueStaffSwitchAfterObservation(
                    currentState,
                    snapshot,
                    currentTime,
                    panelTransition,
                    staffSwitch,
                    spellCast);
            }

            if (spellCast is
                {
                    Status: SpellCastStatus.WaitingForPanel
                } &&
                spellCast.Plan.SelectedSpell?.Panel is { } spellPanel &&
                switchIntent.TargetPanel.IsEquivalentTo(spellPanel))
            {
                return ContinueSpellCastAfterPanel(
                    currentState,
                    spellCast,
                    snapshot,
                    currentTime,
                    panelTransition,
                    staffSwitch);
            }

            if (skillUse is
                {
                    Status: SkillUseStatus.WaitingForPanel
                } &&
                skillUse.Plan.SelectedSkill?.Panel is { } skillPanel &&
                switchIntent.TargetPanel.IsEquivalentTo(skillPanel))
            {
                return ContinueSkillUseAfterPanel(
                    currentState,
                    skillUse,
                    snapshot,
                    currentTime,
                    panelTransition,
                    disarm);
            }
        }

        if (!clientLoggedOut &&
            CanConfirmInventoryMode(currentState.PendingAction, snapshot) &&
            staffSwitch is
            {
                Status: StaffSwitchStatus.ChangingInventoryMode,
                Selection: not null
            })
        {
            pendingAction = null;
            return ContinueStaffSwitchAfterObservation(
                currentState,
                snapshot,
                currentTime,
                panelTransition,
                staffSwitch,
                spellCast);
        }

        if (!clientLoggedOut &&
            CanConfirmDisarm(currentState.PendingAction, snapshot))
        {
            pendingAction = null;
            disarm = currentState.Disarm?.Succeeded();

            if (skillUse is
                {
                    Status: SkillUseStatus.WaitingForDisarm
                } &&
                disarm is not null)
            {
                return ContinueSkillUseAfterDisarm(
                    currentState,
                    skillUse,
                    snapshot,
                    currentTime,
                    panelTransition,
                    disarm);
            }
        }

        if (!clientLoggedOut &&
            CanConfirmStaffEquipment(currentState.PendingAction, snapshot))
        {
            pendingAction = null;
            staffSwitch = currentState.StaffSwitch?.Succeeded();

            if (spellCast is
                {
                    Status: SpellCastStatus.WaitingForStaff
                } &&
                staffSwitch is not null)
            {
                return ContinueSpellCastAfterStaff(
                    currentState,
                    spellCast,
                    snapshot,
                    currentTime,
                    panelTransition,
                    staffSwitch);
            }
        }

        return Changed(
            currentState,
            lifecycle,
            stopReason,
            snapshot,
            lastTransitionAt,
            pendingAction,
            panelTransition: panelTransition,
            staffSwitch: staffSwitch,
            spellCast: spellCast,
            skillUse: skillUse,
            disarm: disarm,
            dialog: dialog,
            flower: flower,
            panelPreservation: panelPreservation);
    }

    private static bool TryGetObservationChange(
        MacroState currentState,
        ClientSnapshot snapshot,
        out ObservationChangeAction action,
        out MacroStopReason stopReason)
    {
        action = ObservationChangeAction.Continue;
        stopReason = MacroStopReason.None;

        if (currentState.Lifecycle != MacroLifecycle.Running ||
            currentState.LatestSnapshot?.Location is not { } previous ||
            snapshot.Presence != ClientPresence.InWorld ||
            snapshot.Location is not { } current)
        {
            return false;
        }

        if (previous.MapNumber != current.MapNumber ||
            !string.Equals(
                previous.MapName,
                current.MapName,
                StringComparison.Ordinal))
        {
            action = currentState.Automation.ObservationChanges.MapChange;
            stopReason = MacroStopReason.MapChanged;
            return true;
        }

        if (previous.X != current.X || previous.Y != current.Y)
        {
            action = currentState.Automation.ObservationChanges.CoordinateChange;
            stopReason = MacroStopReason.CoordinatesChanged;
            return true;
        }

        return false;
    }

    private static MacroDecision RequestPanelTransition(
        MacroState currentState,
        RequestPanelTransitionCommand command,
        MacroTimestamp currentTime)
    {
        if (currentState.Lifecycle != MacroLifecycle.Running ||
            currentState.LatestSnapshot is not
            {
                Presence: ClientPresence.InWorld
            } latestSnapshot)
        {
            return Unchanged(currentState);
        }

        if (currentState.StaffSwitch is
            {
                Status: StaffSwitchStatus.WaitingForInventory
            } ||
            currentState.SpellCast is
            {
                Status: SpellCastStatus.WaitingForPanel
            } ||
            currentState.SkillUse is
            {
                Status: SkillUseStatus.WaitingForPanel
            })
        {
            return Unchanged(currentState);
        }

        if (latestSnapshot.ActivePanel.IsEquivalentTo(command.TargetPanel))
        {
            if (currentState.PendingAction is not null &&
                currentState.PendingAction.Intent is not SwitchPanelIntent)
            {
                return Unchanged(currentState);
            }

            var transition = PanelTransitionState.Succeeded(
                command.TargetPanel,
                attempt: 0,
                maximumAttempts: 0,
                actionId: null);

            if (currentState.PendingAction is null &&
                currentState.PanelTransition == transition)
            {
                return Unchanged(currentState);
            }

            return Changed(
                currentState,
                currentState.Lifecycle,
                currentState.StopReason,
                currentState.LatestSnapshot,
                currentState.LastTransitionAt,
                pendingAction: null,
                panelTransition: transition);
        }

        if (currentState.PendingAction is { } pendingAction)
        {
            if (pendingAction.Intent is not SwitchPanelIntent switchIntent ||
                switchIntent.TargetPanel == command.TargetPanel)
            {
                return Unchanged(currentState);
            }
        }

        return IssuePanelTransitionAttempt(
            currentState,
            command.TargetPanel,
            command.Policy.AttemptTimeout,
            attempt: 1,
            command.Policy.MaximumAttempts,
            currentTime);
    }

    private static MacroDecision HandleClientActionDeadline(
        MacroState currentState,
        ClientActionDeadlineElapsed deadlineElapsed,
        MacroTimestamp currentTime)
    {
        if (currentState.Lifecycle == MacroLifecycle.Running &&
            currentState.SpellCast is
            {
                Status: SpellCastStatus.Casting,
                ActionId: { } spellActionId,
                CompletesAt: { } completesAt
            } spellCast &&
            spellActionId == deadlineElapsed.ActionId &&
            currentTime >= completesAt &&
            currentState.PendingAction?.Intent.ActionId != spellActionId)
        {
            return HandleSpellCastDeadline(
                currentState,
                spellCast,
                currentTime);
        }

        if (currentState.Lifecycle != MacroLifecycle.Running ||
            currentState.PendingAction is not { } pendingAction ||
            pendingAction.Intent.ActionId != deadlineElapsed.ActionId ||
            currentTime < pendingAction.Deadline)
        {
            return Unchanged(currentState);
        }

        if (!pendingAction.IsIssued)
        {
            return HandleClientActionIssue(
                currentState,
                new ClientActionIssue(
                    pendingAction.Intent.ActionId,
                    ClientActionIssueStatus.TimedOut),
                currentTime);
        }

        return pendingAction.Intent switch
        {
            SwitchPanelIntent switchIntent => HandlePanelDeadline(
                currentState,
                pendingAction,
                switchIntent,
                currentTime),
            EquipWeaponIntent weaponIntent => HandleStaffEquipmentDeadline(
                currentState,
                pendingAction,
                weaponIntent,
                currentTime),
            ExpandInventoryIntent => HandleInventoryModeDeadline(
                currentState,
                pendingAction),
            CollapseInventoryIntent => HandleInventoryModeDeadline(
                currentState,
                pendingAction),
            CastSpellIntent => HandleSpellCastDeadline(
                currentState,
                currentState.SpellCast!,
                currentTime),
            DisarmIntent disarmIntent => HandleDisarmDeadline(
                currentState,
                pendingAction,
                disarmIntent,
                currentTime),
            UseSkillIntent useSkillIntent => HandleSkillActionDeadline(
                currentState,
                pendingAction,
                useSkillIntent,
                currentTime),
            AssailIntent assailIntent => HandleSkillActionDeadline(
                currentState,
                pendingAction,
                assailIntent,
                currentTime),
            CancelDialogIntent cancelDialogIntent =>
                HandleDialogCloseDeadline(
                    currentState,
                    pendingAction,
                    cancelDialogIntent),
            _ => Unchanged(currentState)
        };
    }

    private static MacroDecision HandlePanelDeadline(
        MacroState currentState,
        PendingAction pendingAction,
        SwitchPanelIntent switchIntent,
        MacroTimestamp currentTime)
    {
        if (currentState.PanelTransition is not { } panelTransition)
        {
            return Unchanged(currentState);
        }

        if (pendingAction.Attempt >= pendingAction.MaximumAttempts)
        {
            var staffSwitch = currentState.StaffSwitch is
            {
                Status: StaffSwitchStatus.WaitingForInventory
            } waiting
                ? waiting.PanelUnavailable()
                : currentState.StaffSwitch;
            var spellCast = currentState.SpellCast switch
            {
                {
                    Status: SpellCastStatus.WaitingForStaff
                } waitingForStaff => waitingForStaff.StaffUnavailable(),
                {
                    Status: SpellCastStatus.WaitingForPanel
                } waitingForPanel => waitingForPanel.PanelUnavailable(),
                _ => currentState.SpellCast
            };
            var skillUse = currentState.SkillUse is
            {
                Status: SkillUseStatus.WaitingForPanel
            } waitingForSkillPanel
                ? waitingForSkillPanel.PanelUnavailable()
                : currentState.SkillUse;
            var flower = spellCast?.Origin == SpellCastOrigin.Flower
                ? currentState.Flower?.WithSpellCast(spellCast)
                : currentState.Flower;
            var panelPreservation = currentState.PanelPreservation is
            {
                Status: PanelPreservationStatus.Restoring
            } restoring
                    ? restoring.TimedOut()
                    : currentState.PanelPreservation;

            return Changed(
                currentState,
                currentState.Lifecycle,
                currentState.StopReason,
                currentState.LatestSnapshot,
                currentState.LastTransitionAt,
                pendingAction: null,
                panelTransition: panelTransition.TimedOut(),
                staffSwitch: staffSwitch,
                spellCast: spellCast,
                skillUse: skillUse,
                flower: flower,
                panelPreservation: panelPreservation);
        }

        return IssuePanelTransitionAttempt(
            currentState,
            switchIntent.TargetPanel,
            pendingAction.AttemptTimeout,
            checked(pendingAction.Attempt + 1),
            pendingAction.MaximumAttempts,
            currentTime,
            currentState.StaffSwitch,
            currentState.SpellCast,
            currentState.SpellCooldowns,
            currentState.SkillUse,
            currentState.SkillCooldowns,
            currentState.Disarm,
            panelPreservation: currentState.PanelPreservation);
    }

    private static MacroDecision IssuePanelTransitionAttempt(
        MacroState currentState,
        ClientPanel targetPanel,
        TimeSpan attemptTimeout,
        int attempt,
        int maximumAttempts,
        MacroTimestamp currentTime,
        StaffSwitchState? staffSwitch = null,
        SpellCastState? spellCast = null,
        SpellCooldownState? spellCooldowns = null,
        SkillUseState? skillUse = null,
        SkillCooldownState? skillCooldowns = null,
        DisarmState? disarm = null,
        FlowerState? flower = null,
        PanelPreservationState? panelPreservation = null)
    {
        var actionId = new ClientActionId(currentState.NextClientActionId);
        var intent = new SwitchPanelIntent(actionId, targetPanel);
        var deadline = currentTime.Add(attemptTimeout);
        var pendingAction = new PendingAction(
            intent,
            currentTime,
            deadline,
            attempt,
            maximumAttempts,
            currentState.LatestSnapshot?.Sequence);
        var transition = PanelTransitionState.Pending(
            targetPanel,
            attempt,
            maximumAttempts,
            actionId);

        return Changed(
            currentState,
            currentState.Lifecycle,
            currentState.StopReason,
            currentState.LatestSnapshot,
            currentState.LastTransitionAt,
            pendingAction,
            panelTransition: transition,
            staffSwitch: staffSwitch,
            spellCooldowns: spellCooldowns,
            spellCast: spellCast,
            skillCooldowns: skillCooldowns,
            skillUse: skillUse,
            disarm: disarm,
            flowerSchedules: flower?.Plan.Schedules,
            flower: flower,
            panelPreservation: panelPreservation,
            nextClientActionId: checked(currentState.NextClientActionId + 1),
            intent: intent,
            scheduledEvents:
            [
                new ScheduledMacroEvent(
                    new ClientActionDeadlineElapsed(actionId),
                    deadline)
            ]);
    }

    private static bool CanConfirmPanelTransition(
        PendingAction? pendingAction,
        ClientSnapshot snapshot)
    {
        if (pendingAction?.Intent is not SwitchPanelIntent switchIntent ||
            !snapshot.ActivePanel.IsEquivalentTo(switchIntent.TargetPanel))
        {
            return false;
        }

        return CanSnapshotConfirmAction(pendingAction, snapshot);
    }

    private static bool CanSnapshotConfirmAction(
        PendingAction pendingAction,
        ClientSnapshot snapshot)
    {
        if (pendingAction.IssuedAt is not { } issuedAt ||
            snapshot.CaptureStartedAt <= issuedAt)
        {
            return false;
        }

        return pendingAction.BaselineSnapshotSequence is not { } baseline ||
               snapshot.Sequence > baseline;
    }

    private static PanelTransitionState? CancelPendingPanelTransition(
        MacroState currentState) =>
        currentState.PendingAction?.Intent is SwitchPanelIntent &&
        currentState.PanelTransition is
        {
            Status: PanelTransitionStatus.Pending
        } transition
            ? transition.Cancelled()
            : currentState.PanelTransition;

    private static PanelPreservationState? CancelPendingPanelPreservation(
        MacroState currentState) =>
        currentState.PanelPreservation is
        {
            IsActive: true
        } preservation
            ? preservation.Cancelled()
            : currentState.PanelPreservation;

    private static MacroDecision ChangeQueues(
        MacroState currentState,
        ReplaceQueuesCommand command,
        MacroTimestamp currentTime)
    {
        var spellTargetRotations =
            currentState.SpellTargetRotations.Synchronize(
                command.SpellQueue.Entries.Select(entry =>
                    KeyValuePair.Create(entry.Id.Value, entry.Target)));
        var flowerSchedules = currentState.FlowerSchedules.Synchronize(
            command.FlowerQueue,
            currentTime);
        var flowerTargetRotations =
            currentState.FlowerTargetRotations.Synchronize(
                command.FlowerQueue.Entries.Select(entry =>
                    KeyValuePair.Create(entry.Id.Value, entry.Target)));
        if (currentState.SpellQueue.Equals(command.SpellQueue) &&
            currentState.SkillQueue.Equals(command.SkillQueue) &&
            currentState.FlowerQueue.Equals(command.FlowerQueue) &&
            currentState.SpellTargetRotations.Equals(spellTargetRotations) &&
            currentState.FlowerSchedules.Equals(flowerSchedules) &&
            currentState.FlowerTargetRotations.Equals(flowerTargetRotations))
        {
            return Unchanged(currentState);
        }

        return Changed(
            currentState,
            currentState.Lifecycle,
            currentState.StopReason,
            currentState.LatestSnapshot,
            currentState.LastTransitionAt,
            currentState.PendingAction,
            command.SpellQueue,
            skillQueue: command.SkillQueue,
            flowerQueue: command.FlowerQueue,
            flowerSchedules: flowerSchedules,
            spellTargetRotations: spellTargetRotations,
            flowerTargetRotations: flowerTargetRotations);
    }

    private static MacroDecision ApplyAutomationSetup(
        MacroState currentState,
        ApplyAutomationSetupCommand command,
        MacroTimestamp currentTime)
    {
        var queues = command.Queues;
        var spellTargetRotations =
            currentState.SpellTargetRotations.Synchronize(
                queues.SpellQueue.Entries.Select(entry =>
                    KeyValuePair.Create(
                        entry.Id.Value,
                        entry.Target)));
        var flowerSchedules = currentState.FlowerSchedules.Synchronize(
            queues.FlowerQueue,
            currentTime);
        var flowerTargetRotations =
            currentState.FlowerTargetRotations.Synchronize(
                queues.FlowerQueue.Entries.Select(entry =>
                    KeyValuePair.Create(
                        entry.Id.Value,
                        entry.Target)));
        var panelPreservation =
            command.Configuration.PanelPreservation.Enabled
                ? currentState.PanelPreservation
                : CancelPendingPanelPreservation(currentState);
        if (currentState.SpellQueue.Equals(queues.SpellQueue) &&
            currentState.SkillQueue.Equals(queues.SkillQueue) &&
            currentState.FlowerQueue.Equals(queues.FlowerQueue) &&
            currentState.SpellTargetRotations.Equals(
                spellTargetRotations) &&
            currentState.FlowerSchedules.Equals(flowerSchedules) &&
            currentState.FlowerTargetRotations.Equals(
                flowerTargetRotations) &&
            currentState.Automation.Equals(command.Configuration) &&
            Equals(
                currentState.PanelPreservation,
                panelPreservation))
        {
            return Unchanged(currentState);
        }

        return Changed(
            currentState,
            currentState.Lifecycle,
            currentState.StopReason,
            currentState.LatestSnapshot,
            currentState.LastTransitionAt,
            currentState.PendingAction,
            queues.SpellQueue,
            skillQueue: queues.SkillQueue,
            flowerQueue: queues.FlowerQueue,
            flowerSchedules: flowerSchedules,
            spellTargetRotations: spellTargetRotations,
            flowerTargetRotations: flowerTargetRotations,
            automation: command.Configuration,
            panelPreservation: panelPreservation);
    }

    private static MacroDecision ChangeSpellQueue(
        MacroState currentState,
        SpellQueueState spellQueue)
    {
        var targetRotations = currentState.SpellTargetRotations.Synchronize(
            spellQueue.Entries.Select(entry =>
                KeyValuePair.Create(entry.Id.Value, entry.Target)));
        if (currentState.SpellQueue.Equals(spellQueue) &&
            currentState.SpellTargetRotations.Equals(targetRotations))
        {
            return Unchanged(currentState);
        }

        return Changed(
            currentState,
            currentState.Lifecycle,
            currentState.StopReason,
            currentState.LatestSnapshot,
            currentState.LastTransitionAt,
            currentState.PendingAction,
            spellQueue,
            spellTargetRotations: targetRotations);
    }

    private static MacroDecision ChangeSkillQueue(
        MacroState currentState,
        SkillQueueState skillQueue)
    {
        if (currentState.SkillQueue.Equals(skillQueue))
        {
            return Unchanged(currentState);
        }

        return Changed(
            currentState,
            currentState.Lifecycle,
            currentState.StopReason,
            currentState.LatestSnapshot,
            currentState.LastTransitionAt,
            currentState.PendingAction,
            skillQueue: skillQueue);
    }

    private static MacroDecision Changed(
        MacroState currentState,
        MacroLifecycle lifecycle,
        MacroStopReason stopReason,
        ClientSnapshot? latestSnapshot,
        MacroTimestamp? lastTransitionAt,
        PendingAction? pendingAction,
        SpellQueueState? spellQueue = null,
        PanelTransitionState? panelTransition = null,
        long? nextClientActionId = null,
        MacroIntent? intent = null,
        ImmutableArray<ScheduledMacroEvent> scheduledEvents = default,
        StaffSwitchState? staffSwitch = null,
        SpellCooldownState? spellCooldowns = null,
        SpellCastState? spellCast = null,
        SkillQueueState? skillQueue = null,
        SkillCooldownState? skillCooldowns = null,
        SkillUseState? skillUse = null,
        DisarmState? disarm = null,
        DialogState? dialog = null,
        FlowerQueueState? flowerQueue = null,
        FlowerScheduleState? flowerSchedules = null,
        ClientRosterSnapshot? clientRoster = null,
        FlowerState? flower = null,
        TargetRotationState? spellTargetRotations = null,
        TargetRotationState? flowerTargetRotations = null,
        ClientActionIssue? lastActionIssue = null,
        AutomationConfiguration? automation = null,
        PanelPreservationState? panelPreservation = null)
    {
        if (scheduledEvents.IsDefault)
        {
            scheduledEvents = ImmutableArray<ScheduledMacroEvent>.Empty;
        }

        var nextState = new MacroState(
            checked(currentState.Revision + 1),
            lifecycle,
            stopReason,
            latestSnapshot,
            lastTransitionAt,
            pendingAction,
            spellQueue ?? currentState.SpellQueue,
            panelTransition ?? currentState.PanelTransition,
            nextClientActionId ?? currentState.NextClientActionId,
            staffSwitch ?? currentState.StaffSwitch,
            spellCooldowns ?? currentState.SpellCooldowns,
            spellCast ?? currentState.SpellCast,
            skillQueue ?? currentState.SkillQueue,
            skillCooldowns ?? currentState.SkillCooldowns,
            skillUse ?? currentState.SkillUse,
            disarm ?? currentState.Disarm,
            dialog ?? currentState.Dialog,
            flowerQueue ?? currentState.FlowerQueue,
            flowerSchedules ?? currentState.FlowerSchedules,
            clientRoster ?? currentState.ClientRoster,
            flower ?? currentState.Flower,
            spellTargetRotations ?? currentState.SpellTargetRotations,
            flowerTargetRotations ?? currentState.FlowerTargetRotations,
            lastActionIssue ?? currentState.LastActionIssue,
            automation ?? currentState.Automation,
            panelPreservation ?? currentState.PanelPreservation);

        return new MacroDecision(
            nextState,
            ImmutableArray<MacroEvent>.Empty,
            scheduledEvents,
            intent,
            MacroViewSnapshot.FromState(nextState));
    }

    private static MacroDecision Unchanged(MacroState currentState) =>
        new(
            currentState,
            ImmutableArray<MacroEvent>.Empty,
            ImmutableArray<ScheduledMacroEvent>.Empty,
            intent: null,
            publishedView: null);
}
