using System.Collections.Immutable;
using SleepHunter.Runtime.Actions;
using SleepHunter.Runtime.Automation.Panels;
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
            _ => throw new ArgumentOutOfRangeException(
                nameof(input),
                input,
                "Unsupported macro event.")
        };

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
            staffSwitch: CancelPendingStaffSwitch(currentState));
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
                : CancelPendingStaffSwitch(currentState));
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

            if (staffSwitch is
                {
                    Status: StaffSwitchStatus.WaitingForInventory,
                    Selection: { } selection
                } &&
                switchIntent.TargetPanel == ClientPanel.Inventory)
            {
                if (!IsStaffSelectionStillValid(selection, snapshot))
                {
                    return Changed(
                        currentState,
                        lifecycle,
                        stopReason,
                        snapshot,
                        lastTransitionAt,
                        pendingAction: null,
                        panelTransition: panelTransition,
                        staffSwitch: staffSwitch.SelectionInvalidated());
                }

                return IssueStaffEquipmentAttempt(
                    currentState,
                    selection,
                    staffSwitch.AttemptTimeout,
                    checked(staffSwitch.Attempt + 1),
                    staffSwitch.MaximumAttempts,
                    currentTime,
                    snapshot,
                    panelTransition);
            }
        }

        if (!clientLoggedOut &&
            CanConfirmStaffEquipment(currentState.PendingAction, snapshot))
        {
            pendingAction = null;
            staffSwitch = currentState.StaffSwitch?.Succeeded();
        }

        return Changed(
            currentState,
            lifecycle,
            stopReason,
            snapshot,
            lastTransitionAt,
            pendingAction,
            panelTransition: panelTransition,
            staffSwitch: staffSwitch);
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
        if (currentState.Lifecycle != MacroLifecycle.Running ||
            currentState.PendingAction is not { } pendingAction ||
            pendingAction.Intent.ActionId != deadlineElapsed.ActionId ||
            currentTime < pendingAction.Deadline)
        {
            return Unchanged(currentState);
        }

        return pendingAction.Intent switch
        {
            SwitchPanelIntent switchIntent => HandlePanelDeadline(
                currentState,
                pendingAction,
                switchIntent,
                currentTime),
            SetEquippedWeaponIntent weaponIntent => HandleStaffEquipmentDeadline(
                currentState,
                pendingAction,
                weaponIntent,
                currentTime),
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

            return Changed(
                currentState,
                currentState.Lifecycle,
                currentState.StopReason,
                currentState.LatestSnapshot,
                currentState.LastTransitionAt,
                pendingAction: null,
                panelTransition: panelTransition.TimedOut(),
                staffSwitch: staffSwitch);
        }

        return IssuePanelTransitionAttempt(
            currentState,
            switchIntent.TargetPanel,
            pendingAction.AttemptTimeout,
            checked(pendingAction.Attempt + 1),
            pendingAction.MaximumAttempts,
            currentTime,
            currentState.StaffSwitch);
    }

    private static MacroDecision IssuePanelTransitionAttempt(
        MacroState currentState,
        ClientPanel targetPanel,
        TimeSpan attemptTimeout,
        int attempt,
        int maximumAttempts,
        MacroTimestamp currentTime,
        StaffSwitchState? staffSwitch = null)
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
        if (snapshot.CaptureStartedAt <= pendingAction.IssuedAt)
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

    private static MacroDecision ChangeSpellQueue(
        MacroState currentState,
        SpellQueueState spellQueue)
    {
        if (currentState.SpellQueue.Equals(spellQueue))
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
            spellQueue);
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
        StaffSwitchState? staffSwitch = null)
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
            staffSwitch ?? currentState.StaffSwitch);

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
