using System.Collections.Immutable;
using SleepHunter.Runtime.Automation;
using SleepHunter.Runtime.Automation.Dialogs;
using SleepHunter.Runtime.Automation.Panels;
using SleepHunter.Runtime.Automation.Spells;
using SleepHunter.Runtime.Commands;
using SleepHunter.Runtime.Events;
using SleepHunter.Runtime.Intents;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Engine;

public sealed partial class MacroEngine
{
    private static readonly ImmutableArray<AutomationCategory>
        FlowerFirstCategories =
        [
            AutomationCategory.Flowering,
            AutomationCategory.Spells,
            AutomationCategory.Skills
        ];

    private static readonly ImmutableArray<AutomationCategory>
        SpellFirstCategories =
        [
            AutomationCategory.Spells,
            AutomationCategory.Flowering,
            AutomationCategory.Skills
        ];

    private static MacroDecision ConfigureAutomation(
        MacroState currentState,
        ConfigureAutomationCommand command)
    {
        if (currentState.Automation == command.Configuration)
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
            automation: command.Configuration,
            panelPreservation:
                command.Configuration.PanelPreservation.Enabled
                    ? currentState.PanelPreservation
                    : CancelPendingPanelPreservation(currentState));
    }

    private static MacroDecision RunAutomationCycle(
        MacroState currentState,
        MacroTimestamp currentTime)
    {
        var configuration = currentState.Automation;
        var isSpellCasting = currentState.SpellCast is
        {
            Status: SpellCastStatus.Casting
        };
        if (!configuration.IsEnabled ||
            currentState.Lifecycle != MacroLifecycle.Running ||
            currentState.PendingAction is not null ||
            currentState.Dialog is
            {
                Status: DialogStatus.AwaitingObservation
            } ||
            currentState.LatestSnapshot is not
            {
                Presence: ClientPresence.InWorld
            } snapshot ||
            snapshot.IsChatOpen ||
            (!isSpellCasting &&
             IsAutomationSnapshotStale(currentState, snapshot)))
        {
            return Unchanged(currentState);
        }

        var state = currentState;
        var raisedEvents = ImmutableArray.CreateBuilder<MacroEvent>();
        var scheduledEvents =
            ImmutableArray.CreateBuilder<ScheduledMacroEvent>();
        MacroIntent? intent = null;

        if (state.PanelPreservation is
            {
                Status: PanelPreservationStatus.Tracking
            })
        {
            var restoration = RestorePreservedPanel(
                state,
                snapshot,
                currentTime);
            MacroDecisionInvariants.EnsureValid(
                state,
                restoration,
                currentTime);
            if (restoration.Intent is not null ||
                restoration.State.PendingAction is not null)
            {
                return restoration;
            }

            state = restoration.State;
        }

        var categories = configuration.FlowerBeforeSpells
            ? FlowerFirstCategories
            : SpellFirstCategories;
        foreach (var category in categories)
        {
            if (!IsEnabled(configuration, category))
            {
                continue;
            }

            if (isSpellCasting &&
                category != AutomationCategory.Skills)
            {
                continue;
            }

            var decision = RunCategory(
                state,
                configuration,
                category,
                currentTime);
            MacroDecisionInvariants.EnsureValid(
                state,
                decision,
                currentTime);
            state = decision.State;
            raisedEvents.AddRange(decision.RaisedEvents);
            scheduledEvents.AddRange(decision.ScheduledEvents);
            intent = decision.Intent;

            if (intent is not null || state.PendingAction is not null)
            {
                if (configuration.PanelPreservation.Enabled &&
                    snapshot.ActivePanel != ClientPanel.Unknown)
                {
                    state = state.WithPanelPreservation(
                        PanelPreservationState.Tracking(
                            snapshot.ActivePanel,
                            configuration.PanelPreservation.Transition));
                }

                break;
            }
        }

        if (currentState.HasSameContent(state))
        {
            return Unchanged(currentState);
        }

        state = state.WithRevision(
            checked(currentState.Revision + 1));
        return new MacroDecision(
            state,
            raisedEvents.ToImmutable(),
            scheduledEvents.ToImmutable(),
            intent,
            MacroViewSnapshot.FromState(state));
    }

    private static MacroDecision RestorePreservedPanel(
        MacroState currentState,
        ClientSnapshot snapshot,
        MacroTimestamp currentTime)
    {
        var preservation = currentState.PanelPreservation!;
        if (snapshot.ActivePanel.IsEquivalentTo(
                preservation.OriginalPanel))
        {
            return Changed(
                currentState,
                currentState.Lifecycle,
                currentState.StopReason,
                snapshot,
                currentState.LastTransitionAt,
                pendingAction: null,
                panelPreservation: preservation.Succeeded());
        }

        return IssuePanelTransitionAttempt(
            currentState,
            preservation.OriginalPanel,
            preservation.Transition.AttemptTimeout,
            attempt: 1,
            preservation.Transition.MaximumAttempts,
            currentTime,
            panelPreservation: preservation.Restoring());
    }

    private static MacroDecision RunCategory(
        MacroState currentState,
        AutomationConfiguration configuration,
        AutomationCategory category,
        MacroTimestamp currentTime) =>
        category switch
        {
            AutomationCategory.Spells => CastNextSpell(
                currentState,
                new CastNextSpellCommand(
                    configuration.SpellPolicy,
                    configuration.SpellStaffCatalog),
                currentTime),
            AutomationCategory.Skills => UseNextSkill(
                currentState,
                new UseNextSkillCommand(configuration.SkillPolicy),
                currentTime),
            AutomationCategory.Flowering => Flower(
                currentState,
                new FlowerCommand(
                    configuration.FlowerPolicy,
                    configuration.FlowerStaffCatalog),
                currentTime),
            _ => throw new ArgumentOutOfRangeException(
                nameof(category),
                category,
                "The automation category is not supported.")
        };

    private static bool IsEnabled(
        AutomationConfiguration configuration,
        AutomationCategory category) =>
        category switch
        {
            AutomationCategory.Spells => configuration.SpellsEnabled,
            AutomationCategory.Skills => configuration.SkillsEnabled,
            AutomationCategory.Flowering =>
                configuration.FloweringEnabled,
            _ => false
        };

    private static bool IsAutomationSnapshotStale(
        MacroState state,
        ClientSnapshot snapshot)
    {
        var spellRequiredAfter =
            state.SpellCast?.SnapshotRequiredAfter;
        var skillRequiredAfter =
            state.SkillUse?.SnapshotRequiredAfter;
        var requiredAfter = (spellRequiredAfter, skillRequiredAfter) switch
        {
            ({ } spell, { } skill) =>
                spell >= skill ? spell : skill,
            ({ } spell, null) => spell,
            (null, { } skill) => skill,
            _ => (MacroTimestamp?)null
        };

        return requiredAfter is { } required &&
               snapshot.CaptureStartedAt <= required;
    }

    private static MacroDecision RequestAutomationCycleAfter(
        MacroState previousState,
        MacroEvent input,
        MacroDecision decision)
    {
        if (decision.State.Revision == previousState.Revision ||
            decision.State.Lifecycle != MacroLifecycle.Running ||
            decision.State.PendingAction is not null ||
            decision.State.Dialog is
            {
                Status: DialogStatus.AwaitingObservation
            } ||
            decision.Intent is not null ||
            !decision.State.Automation.IsEnabled ||
            WasPreservedPanelRestored(previousState, decision.State) ||
            !ShouldRequestAutomationCycle(input))
        {
            return decision;
        }

        return new MacroDecision(
            decision.State,
            decision.RaisedEvents.Add(
                new AutomationCycleRequested()),
            decision.ScheduledEvents,
            decision.Intent,
            decision.PublishedView);
    }

    private static bool WasPreservedPanelRestored(
        MacroState previousState,
        MacroState nextState) =>
        previousState.PanelPreservation is
        {
            Status: PanelPreservationStatus.Restoring
        } &&
        nextState.PanelPreservation is
        {
            Status: PanelPreservationStatus.Succeeded
        };

    private static bool ShouldRequestAutomationCycle(MacroEvent input) =>
        input switch
        {
            ClientSnapshotObserved => true,
            ClientRosterObserved => true,
            MacroCommandReceived
            {
                Command: not (
                    PauseMacroCommand or
                    StopMacroCommand or
                    CastNextSpellCommand or
                    UseNextSkillCommand or
                    FlowerCommand)
            } => true,
            _ => false
        };

    private enum AutomationCategory
    {
        Spells,
        Skills,
        Flowering
    }
}
