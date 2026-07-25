using System.Collections.Immutable;
using SleepHunter.Runtime.Automation;
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
            automation: command.Configuration);
    }

    private static MacroDecision RunAutomationCycle(
        MacroState currentState,
        MacroTimestamp currentTime)
    {
        var configuration = currentState.Automation;
        if (!configuration.IsEnabled ||
            currentState.Lifecycle != MacroLifecycle.Running ||
            currentState.PendingAction is not null ||
            currentState.LatestSnapshot is not
            {
                Presence: ClientPresence.InWorld
            } snapshot ||
            snapshot.IsUserChatting ||
            IsAutomationSnapshotStale(currentState, snapshot))
        {
            return Unchanged(currentState);
        }

        var categories = configuration.FlowerBeforeSpells
            ? FlowerFirstCategories
            : SpellFirstCategories;
        var state = currentState;
        var raisedEvents = ImmutableArray.CreateBuilder<MacroEvent>();
        var scheduledEvents =
            ImmutableArray.CreateBuilder<ScheduledMacroEvent>();
        MacroIntent? intent = null;

        foreach (var category in categories)
        {
            if (!IsEnabled(configuration, category))
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
            decision.Intent is not null ||
            !decision.State.Automation.IsEnabled ||
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
