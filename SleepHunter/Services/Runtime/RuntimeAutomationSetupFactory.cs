using System;
using System.Collections.Generic;
using SleepHunter.Macro;
using SleepHunter.Persistence.Configuration;
using SleepHunter.Runtime.Automation;
using SleepHunter.Runtime.Automation.Flowering;
using SleepHunter.Runtime.Automation.Panels;
using SleepHunter.Runtime.Automation.Skills;
using SleepHunter.Runtime.Automation.Spells;
using SleepHunter.Runtime.Characters;
using SleepHunter.Runtime.Commands;
using SleepHunter.Settings;

namespace SleepHunter.Services.Runtime
{
    public sealed class RuntimeAutomationSetupFactory :
        IRuntimeAutomationSetupFactory
    {
        private static readonly TimeSpan CompletionPadding =
            TimeSpan.FromMilliseconds(100);

        private readonly IRuntimeStaffCandidateProvider staffCandidates;

        public RuntimeAutomationSetupFactory(
            IRuntimeStaffCandidateProvider staffCandidates)
        {
            this.staffCandidates = staffCandidates ??
                throw new ArgumentNullException(nameof(staffCandidates));
        }

        public RuntimeAutomationSetup Create(
            MacroConfiguration configuration,
            UserSettings settings,
            CharacterClass characterClass)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentNullException.ThrowIfNull(settings);
            if (!Enum.IsDefined(characterClass))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(characterClass),
                    characterClass,
                    "The runtime character class is not supported.");
            }

            var rotation =
                configuration.SpellRotation ??
                ToSpellQueueRotation(settings.SpellRotationMode);
            var replaceQueues = new ReplaceQueuesCommand(
                configuration.Spells,
                rotation,
                configuration.Skills,
                configuration.Flowers);
            var spellPolicy = CreateSpellPolicy(settings);
            var flowerOptions = configuration.FlowerOptions;
            var flowerPolicy = new FlowerExecutionPolicy(
                new FlowerTargetPolicy(
                    flowerOptions.FlowerAlternateCharacters,
                    settings.FlowerAltsFirst,
                    flowerOptions.MaximumXDistance,
                    flowerOptions.MaximumYDistance),
                spellPolicy,
                useVineyard: flowerOptions.UseVineyard,
                restoreMana: settings.UseFasSpiorad,
                restoreManaOnDemand: settings.UseFasSpioradOnDemand,
                manaRestorationThreshold:
                    ToManaThreshold(settings.FasSpioradThreshold),
                minimumManaBeforePlant: settings.FlowerHasMinimum
                    ? settings.FlowerMinimumMana
                    : null);
            var skillPolicy = new SkillExecutionPolicy(
                new SkillUsePolicy(
                    requireMana: false,
                    settings.UseSpaceForAssail
                        ? AssailMode.SpaceBar
                        : AssailMode.SkillSlot,
                    settings.DisarmForAssails));
            var flowerEnabled =
                !configuration.Flowers.IsEmpty ||
                flowerOptions.FlowerAlternateCharacters ||
                flowerOptions.UseVineyard;
            var automation = new AutomationConfiguration(
                spellsEnabled: !configuration.Spells.IsEmpty,
                skillsEnabled: !configuration.Skills.IsEmpty,
                floweringEnabled: flowerEnabled,
                flowerBeforeSpells: settings.FlowerBeforeSpellMacros,
                spellPolicy: spellPolicy,
                spellStaffCatalog: CreateSpellStaffCatalog(
                    configuration,
                    settings,
                    characterClass),
                skillPolicy: skillPolicy,
                flowerPolicy: flowerPolicy,
                flowerStaffCatalog: CreateFlowerStaffCatalog(
                    settings,
                    characterClass),
                observationChanges: new ObservationChangePolicy(
                    ToObservationChangeAction(settings.MapChangeAction),
                    ToObservationChangeAction(settings.CoordsChangeAction)),
                panelPreservation: new PanelPreservationPolicy(
                    settings.PreserveUserPanel));

            return new RuntimeAutomationSetup(
                replaceQueues,
                new ConfigureAutomationCommand(automation));
        }

        private SpellStaffCatalog CreateSpellStaffCatalog(
            MacroConfiguration configuration,
            UserSettings settings,
            CharacterClass characterClass)
        {
            if (!settings.AllowStaffSwitching)
            {
                return SpellStaffCatalog.Empty;
            }

            var candidateSets = new List<SpellStaffCandidateSet>();
            foreach (var entry in configuration.Spells)
            {
                var candidates = staffCandidates.GetCandidates(
                    entry.Name,
                    characterClass);
                if (!candidates.IsEmpty)
                {
                    candidateSets.Add(
                        new SpellStaffCandidateSet(
                            entry.Id,
                            candidates));
                }
            }

            return new SpellStaffCatalog(candidateSets);
        }

        private FlowerStaffCatalog CreateFlowerStaffCatalog(
            UserSettings settings,
            CharacterClass characterClass)
        {
            if (!settings.AllowStaffSwitching)
            {
                return FlowerStaffCatalog.Empty;
            }

            var candidateSets = new List<FlowerStaffCandidateSet>();
            AddFlowerStaffCandidates(
                candidateSets,
                FlowerActionKind.RestoreMana,
                FlowerSpellNames.ManaRestoration,
                characterClass);
            AddFlowerStaffCandidates(
                candidateSets,
                FlowerActionKind.Vineyard,
                FlowerSpellNames.Vineyard,
                characterClass);
            AddFlowerStaffCandidates(
                candidateSets,
                FlowerActionKind.Plant,
                FlowerSpellNames.Plant,
                characterClass);
            return new FlowerStaffCatalog(candidateSets);
        }

        private void AddFlowerStaffCandidates(
            ICollection<FlowerStaffCandidateSet> candidateSets,
            FlowerActionKind action,
            string spellName,
            CharacterClass characterClass)
        {
            var candidates = staffCandidates.GetCandidates(
                spellName,
                characterClass);
            if (!candidates.IsEmpty)
            {
                candidateSets.Add(
                    new FlowerStaffCandidateSet(
                        action,
                        candidates));
            }
        }

        private static SpellExecutionPolicy CreateSpellPolicy(
            UserSettings settings) =>
            new(
                new SpellCastPolicy(
                    settings.RequireManaForSpells,
                    new SpellCastTimingPolicy(
                        settings.ZeroLineDelay,
                        settings.SingleLineDelay,
                        settings.MultipleLineDelay,
                        CompletionPadding),
                    skipCoolingDownSpells:
                        settings.SkipSpellsOnCooldown),
                allowStaffSwitching: settings.AllowStaffSwitching);

        private static int ToManaThreshold(double value)
        {
            if (!double.IsFinite(value) ||
                value < 0 ||
                value > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "Mana restoration thresholds must fit a nonnegative runtime integer.");
            }

            return checked((int)Math.Ceiling(value));
        }

        private static ObservationChangeAction ToObservationChangeAction(
            MacroAction action) =>
            action switch
            {
                MacroAction.None => ObservationChangeAction.Continue,
                MacroAction.Pause => ObservationChangeAction.Pause,
                MacroAction.Stop or MacroAction.ForceQuit =>
                    ObservationChangeAction.Stop,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(action),
                    action,
                    "The macro action cannot be applied to an observed client change.")
            };

        private static SpellQueueRotation ToSpellQueueRotation(
            SpellRotationMode rotation) =>
            rotation switch
            {
                SpellRotationMode.Default or SpellRotationMode.None =>
                    SpellQueueRotation.Priority,
                SpellRotationMode.Singular =>
                    SpellQueueRotation.Sequential,
                SpellRotationMode.RoundRobin =>
                    SpellQueueRotation.RoundRobin,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(rotation),
                    rotation,
                    "The spell rotation mode is not supported.")
            };
    }
}
