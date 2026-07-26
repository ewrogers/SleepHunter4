using System.Collections.Immutable;
using SleepHunter.Runtime.Actions;
using SleepHunter.Runtime.Automation.Dialogs;
using SleepHunter.Runtime.Automation.Equipment;
using SleepHunter.Runtime.Automation.Panels;
using SleepHunter.Runtime.Automation.Skills;
using SleepHunter.Runtime.Commands;
using SleepHunter.Runtime.Events;
using SleepHunter.Runtime.Intents;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Engine;

public sealed partial class MacroEngine
{
    private static MacroDecision UseNextSkill(
        MacroState currentState,
        UseNextSkillCommand command,
        MacroTimestamp currentTime)
    {
        if (currentState.Lifecycle != MacroLifecycle.Running ||
            currentState.PendingAction is not null ||
            currentState.LatestSnapshot is not
            {
                Presence: ClientPresence.InWorld
            } snapshot)
        {
            return Unchanged(currentState);
        }

        var snapshotIsFresh =
            currentState.SkillUse?.SnapshotRequiredAfter is not
            { } requiredAfter ||
            snapshot.CaptureStartedAt > requiredAfter;
        var plan = PlanSkill(
            currentState.SkillQueue,
            snapshot,
            currentState.SkillCooldowns,
            currentTime,
            command.Policy.Planning,
            snapshotIsFresh);
        var skillUse = SkillUseState.FromPlan(
            plan,
            command.Policy,
            currentState.SkillUse?.SnapshotRequiredAfter);

        if (!plan.HasSelection)
        {
            return Changed(
                currentState,
                currentState.Lifecycle,
                currentState.StopReason,
                currentState.LatestSnapshot,
                currentState.LastTransitionAt,
                pendingAction: null,
                skillQueue: plan.Queue,
                skillCooldowns: plan.Cooldowns,
                skillUse: skillUse);
        }

        return ContinueSkillUsePrerequisites(
            currentState,
            skillUse,
            plan,
            snapshot,
            currentTime,
            currentState.PanelTransition,
            currentState.Disarm);
    }

    private static MacroDecision ContinueSkillUseAfterDisarm(
        MacroState currentState,
        SkillUseState skillUse,
        ClientSnapshot snapshot,
        MacroTimestamp currentTime,
        PanelTransitionState? panelTransition,
        DisarmState disarm)
    {
        var plan = ReplanSelectedSkill(
            currentState,
            skillUse,
            snapshot,
            currentTime);
        if (!DoesSkillPlanMatchSelection(skillUse, plan))
        {
            return FinishInvalidSkillSelection(
                currentState,
                skillUse,
                plan,
                snapshot,
                panelTransition,
                disarm);
        }

        return ContinueSkillUsePrerequisites(
            currentState,
            skillUse.WithPlan(plan),
            plan,
            snapshot,
            currentTime,
            panelTransition,
            disarm);
    }

    private static MacroDecision ContinueSkillUseAfterPanel(
        MacroState currentState,
        SkillUseState skillUse,
        ClientSnapshot snapshot,
        MacroTimestamp currentTime,
        PanelTransitionState panelTransition,
        DisarmState? disarm)
    {
        var plan = ReplanSelectedSkill(
            currentState,
            skillUse,
            snapshot,
            currentTime);
        if (!DoesSkillPlanMatchSelection(skillUse, plan))
        {
            return FinishInvalidSkillSelection(
                currentState,
                skillUse,
                plan,
                snapshot,
                panelTransition,
                disarm);
        }

        return ContinueSkillUsePrerequisites(
            currentState,
            skillUse.WithPlan(plan),
            plan,
            snapshot,
            currentTime,
            panelTransition,
            disarm);
    }

    private static MacroDecision ContinueSkillUsePrerequisites(
        MacroState currentState,
        SkillUseState skillUse,
        SkillPlan plan,
        ClientSnapshot snapshot,
        MacroTimestamp currentTime,
        PanelTransitionState? panelTransition,
        DisarmState? disarm)
    {
        if (plan.RequiresDisarm)
        {
            if (snapshot.Equipment is not { } equipment)
            {
                return Changed(
                    currentState,
                    currentState.Lifecycle,
                    currentState.StopReason,
                    snapshot,
                    currentState.LastTransitionAt,
                    pendingAction: null,
                    panelTransition: panelTransition,
                    skillCooldowns: plan.Cooldowns,
                    skillUse: skillUse.SnapshotUnavailable(),
                    disarm: DisarmState.SnapshotUnavailable());
            }

            if (!equipment.IsDisarmed)
            {
                return IssueDisarmAttempt(
                    currentState,
                    skillUse.WaitingForDisarm(plan),
                    plan,
                    attempt: 1,
                    currentTime,
                    snapshot,
                    panelTransition);
            }

            disarm = disarm is { Status: DisarmStatus.Succeeded }
                ? disarm
                : DisarmState.NoChange();
        }

        var selectedSkill = plan.SelectedSkill!;
        if (plan.ActionKind == SkillActionKind.UseSkill &&
            !snapshot.ActivePanel.IsEquivalentTo(selectedSkill.Panel))
        {
            return IssuePanelTransitionAttempt(
                currentState,
                selectedSkill.Panel,
                skillUse.Policy.PanelTransition.AttemptTimeout,
                attempt: 1,
                skillUse.Policy.PanelTransition.MaximumAttempts,
                currentTime,
                spellCast: currentState.SpellCast,
                spellCooldowns: currentState.SpellCooldowns,
                skillUse: skillUse.WaitingForPanel(plan),
                skillCooldowns: plan.Cooldowns,
                disarm: disarm);
        }

        return IssueSkillAction(
            currentState,
            skillUse,
            plan,
            snapshot,
            currentTime,
            panelTransition,
            disarm);
    }

    private static MacroDecision IssueDisarmAttempt(
        MacroState currentState,
        SkillUseState skillUse,
        SkillPlan plan,
        int attempt,
        MacroTimestamp currentTime,
        ClientSnapshot snapshot,
        PanelTransitionState? panelTransition)
    {
        var actionId = new ClientActionId(currentState.NextClientActionId);
        var intent = new DisarmIntent(actionId);
        var policy = skillUse.Policy.Disarm;
        var deadline = currentTime.Add(policy.AttemptTimeout);
        var pendingAction = new PendingAction(
            intent,
            currentTime,
            deadline,
            attempt,
            policy.MaximumAttempts,
            snapshot.Sequence);
        var disarm = DisarmState.Disarming(
            policy.AttemptTimeout,
            attempt,
            policy.MaximumAttempts,
            actionId);

        return Changed(
            currentState,
            currentState.Lifecycle,
            currentState.StopReason,
            snapshot,
            currentState.LastTransitionAt,
            pendingAction,
            panelTransition: panelTransition,
            skillCooldowns: plan.Cooldowns,
            skillUse: skillUse,
            disarm: disarm,
            nextClientActionId: checked(currentState.NextClientActionId + 1),
            intent: intent,
            scheduledEvents:
            [
                new ScheduledMacroEvent(
                    new ClientActionDeadlineElapsed(actionId),
                    deadline)
            ]);
    }

    private static MacroDecision IssueSkillAction(
        MacroState currentState,
        SkillUseState skillUse,
        SkillPlan plan,
        ClientSnapshot snapshot,
        MacroTimestamp currentTime,
        PanelTransitionState? panelTransition,
        DisarmState? disarm)
    {
        var selectedSkill = plan.SelectedSkill!;
        var actionId = new ClientActionId(currentState.NextClientActionId);
        ClientActionIntent intent = plan.ActionKind switch
        {
            SkillActionKind.UseSkill => new UseSkillIntent(
                actionId,
                selectedSkill.Name,
                selectedSkill.Slot,
                selectedSkill.Panel),
            SkillActionKind.Assail => new AssailIntent(
                actionId,
                selectedSkill.Name),
            _ => throw new InvalidOperationException(
                "The selected skill action is not supported.")
        };
        var deadline = currentTime.Add(skillUse.Policy.ActionDuration);
        var pendingAction = new PendingAction(
            intent,
            currentTime,
            deadline,
            attempt: 1,
            maximumAttempts: 1,
            snapshot.Sequence);
        var acting = skillUse.Acting(plan, actionId, deadline);
        var actionDeadline = new ScheduledMacroEvent(
            new ClientActionDeadlineElapsed(actionId),
            deadline);
        var dialog = currentState.Dialog;
        var scheduledEvents = ImmutableArray.Create(actionDeadline);

        if (selectedSkill.OpensDialog)
        {
            var dialogDueAt =
                currentTime.Add(
                    skillUse.Policy.Dialog.ObservationTimeout);
            dialog = DialogState.Scheduled(
                skillUse.Policy.Dialog,
                dialogDueAt);
            scheduledEvents = scheduledEvents.Add(
                new ScheduledMacroEvent(
                    new DialogCloseDue(dialogDueAt),
                    dialogDueAt));
        }

        return Changed(
            currentState,
            currentState.Lifecycle,
            currentState.StopReason,
            snapshot,
            currentState.LastTransitionAt,
            pendingAction,
            panelTransition: panelTransition,
            skillQueue: plan.Queue,
            skillCooldowns: plan.Cooldowns,
            skillUse: acting,
            disarm: disarm,
            dialog: dialog,
            nextClientActionId: checked(currentState.NextClientActionId + 1),
            intent: intent,
            scheduledEvents: scheduledEvents);
    }

    private static MacroDecision HandleDisarmDeadline(
        MacroState currentState,
        PendingAction pendingAction,
        DisarmIntent intent,
        MacroTimestamp currentTime)
    {
        if (currentState.Disarm is not
            {
                Status: DisarmStatus.Disarming
            } disarm ||
            disarm.ActionId != intent.ActionId ||
            currentState.SkillUse is not
            {
                Status: SkillUseStatus.WaitingForDisarm
            } skillUse)
        {
            return Unchanged(currentState);
        }

        if (pendingAction.Attempt >= pendingAction.MaximumAttempts)
        {
            return Changed(
                currentState,
                currentState.Lifecycle,
                currentState.StopReason,
                currentState.LatestSnapshot,
                currentState.LastTransitionAt,
                pendingAction: null,
                skillUse: skillUse.DisarmUnavailable(),
                disarm: disarm.TimedOut());
        }

        if (currentState.LatestSnapshot is not
            {
                Equipment: { IsDisarmed: false }
            } snapshot)
        {
            return Changed(
                currentState,
                currentState.Lifecycle,
                currentState.StopReason,
                currentState.LatestSnapshot,
                currentState.LastTransitionAt,
                pendingAction: null,
                skillUse: skillUse.DisarmUnavailable(),
                disarm: DisarmState.SnapshotUnavailable());
        }

        return IssueDisarmAttempt(
            currentState,
            skillUse,
            skillUse.Plan,
            checked(pendingAction.Attempt + 1),
            currentTime,
            snapshot,
            currentState.PanelTransition);
    }

    private static MacroDecision HandleSkillActionDeadline(
        MacroState currentState,
        PendingAction pendingAction,
        ClientActionIntent intent,
        MacroTimestamp currentTime)
    {
        if (currentState.SkillUse is not
            {
                Status: SkillUseStatus.Using or SkillUseStatus.Assailing,
                Plan.SelectedSkill: { } skill
            } skillUse ||
            skillUse.ActionId != intent.ActionId)
        {
            return Unchanged(currentState);
        }

        var cooldowns = currentState.SkillCooldowns.Prune(currentTime);
        var readyAt = pendingAction.Deadline.Add(skill.Cooldown);
        if (readyAt > currentTime)
        {
            cooldowns = cooldowns.WithCooldown(skill.Name, readyAt);
        }

        return Changed(
            currentState,
            currentState.Lifecycle,
            currentState.StopReason,
            currentState.LatestSnapshot,
            currentState.LastTransitionAt,
            pendingAction: null,
            skillCooldowns: cooldowns,
            skillUse: skillUse.Succeeded());
    }

    private static MacroDecision FinishInvalidSkillSelection(
        MacroState currentState,
        SkillUseState skillUse,
        SkillPlan plan,
        ClientSnapshot snapshot,
        PanelTransitionState? panelTransition,
        DisarmState? disarm)
    {
        var nextSkillUse = plan.HasSelection
            ? skillUse.SelectionInvalidated(plan)
            : skillUse.Replanned(plan);
        var skillQueue = plan.HasSelection
            ? currentState.SkillQueue
            : plan.Queue;

        return Changed(
            currentState,
            currentState.Lifecycle,
            currentState.StopReason,
            snapshot,
            currentState.LastTransitionAt,
            pendingAction: null,
            panelTransition: panelTransition,
            skillQueue: skillQueue,
            skillCooldowns: plan.Cooldowns,
            skillUse: nextSkillUse,
            disarm: disarm);
    }

    private static SkillPlan PlanSkill(
        SkillQueueState queue,
        ClientSnapshot snapshot,
        SkillCooldownState cooldowns,
        MacroTimestamp currentTime,
        SkillUsePolicy policy,
        bool includeSnapshotSections = true) =>
        SkillPlanner.Plan(
            new SkillPlanningRequest(
                queue,
                includeSnapshotSections ? snapshot.Vitals : null,
                includeSnapshotSections ? snapshot.Skillbook : null,
                cooldowns,
                currentTime,
                policy));

    private static SkillPlan ReplanSelectedSkill(
        MacroState currentState,
        SkillUseState skillUse,
        ClientSnapshot snapshot,
        MacroTimestamp currentTime) =>
        PlanSkill(
            currentState.SkillQueue,
            snapshot,
            currentState.SkillCooldowns,
            currentTime,
            skillUse.Policy.Planning);

    private static bool DoesSkillPlanMatchSelection(
        SkillUseState skillUse,
        SkillPlan plan)
    {
        var expectedEntry = skillUse.Plan.SelectedEntry!;
        var expectedSkill = skillUse.Plan.SelectedSkill!;

        return plan.SelectedEntry == expectedEntry &&
               plan.SelectedSkill is { } selectedSkill &&
               selectedSkill.Slot == expectedSkill.Slot &&
               selectedSkill.ManaCost == expectedSkill.ManaCost &&
               selectedSkill.Cooldown == expectedSkill.Cooldown &&
               selectedSkill.IsAssail == expectedSkill.IsAssail &&
               selectedSkill.OpensDialog == expectedSkill.OpensDialog &&
               selectedSkill.RequiresDisarm == expectedSkill.RequiresDisarm &&
               selectedSkill.HealthCondition == expectedSkill.HealthCondition &&
               plan.ActionKind == skillUse.Plan.ActionKind &&
               plan.RequiresDisarm == skillUse.Plan.RequiresDisarm &&
               string.Equals(
                   selectedSkill.Name,
                   expectedSkill.Name,
                   StringComparison.OrdinalIgnoreCase);
    }

    private static bool CanConfirmDisarm(
        PendingAction? pendingAction,
        ClientSnapshot snapshot) =>
        pendingAction?.Intent is DisarmIntent &&
        snapshot.Equipment is { IsDisarmed: true } &&
        CanSnapshotConfirmAction(pendingAction, snapshot);

    private static SkillUseState? CancelPendingSkillUse(
        MacroState currentState) =>
        currentState.SkillUse is
        {
            Status: SkillUseStatus.WaitingForDisarm or
                SkillUseStatus.WaitingForPanel or
                SkillUseStatus.Using or
                SkillUseStatus.Assailing
        } skillUse
            ? skillUse.Cancelled()
            : currentState.SkillUse;

    private static DisarmState? CancelPendingDisarm(
        MacroState currentState) =>
        currentState.Disarm is
        {
            Status: DisarmStatus.Disarming
        } disarm
            ? disarm.Cancelled()
            : currentState.Disarm;
}
